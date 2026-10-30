using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SIVAD.Repositories;
using SIVAD.Seguranca;

namespace SIVAD.Controllers;

public sealed class AutenticacaoController : Controller
{
    private readonly IRepositorioSIVAD _repositorio;
    private readonly IConfiguration _configuracao;
    private readonly ILogger<AutenticacaoController> _registro;
    private readonly IWebHostEnvironment _ambiente;

    public AutenticacaoController(
        IRepositorioSIVAD repositorio,
        IConfiguration configuracao,
        ILogger<AutenticacaoController> registro,
        IWebHostEnvironment ambiente)
    {
        _repositorio = repositorio;
        _configuracao = configuracao;
        _registro = registro;
        _ambiente = ambiente;
    }

    private async Task PrepararLoginAsync()
    {
        try
        {
            ViewBag.PodeConfigurarPrimeiroAdministrador =
                !await _repositorio.ExisteAdministradorConfiguradoAsync();
        }
        catch (SqlException ex)
        {
            _registro.LogWarning(ex, "Não foi possível consultar se a configuração inicial do administrador já foi concluída.");
            // O link fica visível quando o banco não pode ser consultado; a gravação continua protegida no repositório.
            ViewBag.PodeConfigurarPrimeiroAdministrador = true;
        }
    }

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        await PrepararLoginAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string uid, string senha)
    {
        ViewBag.Uid = uid;
        if (!int.TryParse(uid, out var pessoaId) || pessoaId <= 0 || string.IsNullOrWhiteSpace(senha))
        {
            ModelState.AddModelError(string.Empty, "Informe o UID numérico e a senha.");
            await PrepararLoginAsync();
            return View();
        }

        try
        {
            var credencial = await _repositorio.ObterCredencialAcessoAsync(pessoaId);
            if (credencial is null || !GerenciadorSenhas.VerificarSenha(senha, credencial.SenhaHash))
            {
                ModelState.AddModelError(string.Empty, "UID ou senha inválidos.");
                await PrepararLoginAsync();
                return View();
            }

            HttpContext.Session.SetInt32("id", credencial.PessoaId);
            HttpContext.Session.SetString("nome", credencial.Nome);
            HttpContext.Session.SetString("perfil", credencial.Perfil);

            return credencial.Perfil == "Administrador"
                ? RedirectToAction("Dashboard", "Administrador")
                : RedirectToAction("RegistrarPedido", "Funcionario");
        }
        catch (SqlException ex)
        {
            _registro.LogError(ex, "Erro SQL ao validar credenciais. Código {CodigoSql}, estado {EstadoSql}.", ex.Number, ex.State);
            ViewBag.PodeConfigurarPrimeiroAdministrador = true;
            ModelState.AddModelError(string.Empty, _ambiente.IsDevelopment()
                ? $"Falha SQL {ex.Number} (estado {ex.State}). Consulte a saída do terminal e confirme a conexão e a migração."
                : "Não foi possível acessar as credenciais. Verifique a conexão e execute Database/AtualizarBanco.sql.");
            return View();
        }
    }

    [HttpGet]
    public IActionResult RecuperacaoSenha() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecuperacaoSenha(string nomeUsuario, string uid, string email)
    {
        ViewBag.NomeUsuario = nomeUsuario;
        ViewBag.Uid = uid;
        ViewBag.Email = email;

        if (string.IsNullOrWhiteSpace(nomeUsuario) || !int.TryParse(uid, out var pessoaId) ||
            pessoaId <= 0 || string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, "Preencha nome, UID numérico e e-mail cadastrado.");
            return View();
        }

        try
        {
            // A resposta é igual para conta encontrada ou não, evitando revelar cadastros.
            await _repositorio.RegistrarSolicitacaoRecuperacaoSenhaAsync(pessoaId, nomeUsuario, email);
            TempData["Sucesso"] = "Se os dados corresponderem a um funcionário ativo, a solicitação será analisada pelo administrador. Nenhum e-mail automático será enviado.";
            return RedirectToAction(nameof(RecuperacaoSenha));
        }
        catch (SqlException ex)
        {
            _registro.LogError(ex, "Erro SQL ao registrar recuperação. Código {CodigoSql}, estado {EstadoSql}.", ex.Number, ex.State);
            ModelState.AddModelError(string.Empty, _ambiente.IsDevelopment()
                ? $"Falha SQL {ex.Number} (estado {ex.State}). Consulte a saída do terminal e confirme a migração."
                : "Não foi possível registrar a solicitação. Verifique se a migração do banco foi executada.");
            return View();
        }
    }

    [HttpGet]
    public IActionResult ConfigurarPrimeiroAdministrador() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfigurarPrimeiroAdministrador(
        int uid, string cpf, string email, string chaveConfiguracao, string senha, string confirmarSenha)
    {
        var chaveConfigurada = _configuracao["Autenticacao:ChaveConfiguracaoInicial"];
        if (string.IsNullOrWhiteSpace(chaveConfigurada))
        {
            ModelState.AddModelError(string.Empty, "Defina Autenticacao:ChaveConfiguracaoInicial na configuração local antes do primeiro acesso.");
            return View();
        }

        var chaveRecebida = Encoding.UTF8.GetBytes(chaveConfiguracao ?? string.Empty);
        var chaveEsperada = Encoding.UTF8.GetBytes(chaveConfigurada);
        if (!CryptographicOperations.FixedTimeEquals(chaveRecebida, chaveEsperada))
            ModelState.AddModelError(string.Empty, "Chave de configuração inválida.");
        if (uid <= 0 || string.IsNullOrWhiteSpace(cpf) || string.IsNullOrWhiteSpace(email))
            ModelState.AddModelError(string.Empty, "Informe o UID, CPF e e-mail que já constam no cadastro do administrador.");
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 8)
            ModelState.AddModelError(string.Empty, "A senha deve ter pelo menos 8 caracteres.");
        if (!string.Equals(senha, confirmarSenha, StringComparison.Ordinal))
            ModelState.AddModelError(string.Empty, "A confirmação da senha não corresponde.");
        if (!ModelState.IsValid) return View();

        try
        {
            var senhaHash = GerenciadorSenhas.CriarHash(senha);
            var configurado = await _repositorio.ConfigurarPrimeiroAdministradorAsync(uid, cpf, email, senhaHash);
            if (!configurado)
            {
                ModelState.AddModelError(string.Empty, "O UID, CPF ou e-mail não corresponde a um administrador cadastrado, ou a configuração inicial já foi concluída.");
                return View();
            }

            HttpContext.Session.SetInt32("id", uid);
            HttpContext.Session.SetString("nome", "Administrador");
            HttpContext.Session.SetString("perfil", "Administrador");
            return RedirectToAction("Dashboard", "Administrador");
        }
        catch (SqlException ex)
        {
            _registro.LogError(ex, "Erro SQL ao configurar o primeiro administrador. Código {CodigoSql}, estado {EstadoSql}.", ex.Number, ex.State);
            ModelState.AddModelError(string.Empty, _ambiente.IsDevelopment()
                ? $"Falha SQL {ex.Number} (estado {ex.State}). Consulte a saída do terminal; confirme conexão, banco selecionado e migração."
                : "Não foi possível configurar o acesso. Execute Database/AtualizarBanco.sql e confirme a conexão.");
            return View();
        }
    }

    [HttpGet]
    public IActionResult Sair()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}
