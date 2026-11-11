using System.ComponentModel.DataAnnotations;
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
        string nome, string cpf, string email, string chaveConfiguracao, string senha, string confirmarSenha)
    {
        ViewBag.Nome = nome;
        ViewBag.Cpf = cpf;
        ViewBag.Email = email;

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
        var cpfNormalizado = (cpf ?? string.Empty).Trim().Replace(".", "").Replace("-", "").Replace(" ", "");
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 150)
            ModelState.AddModelError(string.Empty, "Informe o nome do administrador (até 150 caracteres).");
        if (cpfNormalizado.Length != 11 || cpfNormalizado.Any(caractere => caractere < '0' || caractere > '9'))
            ModelState.AddModelError(string.Empty, "Informe um CPF com 11 números.");
        if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 100 || !new EmailAddressAttribute().IsValid(email.Trim()))
            ModelState.AddModelError(string.Empty, "Informe um e-mail válido com até 100 caracteres.");
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 8)
            ModelState.AddModelError(string.Empty, "A senha deve ter pelo menos 8 caracteres.");
        if (!string.Equals(senha, confirmarSenha, StringComparison.Ordinal))
            ModelState.AddModelError(string.Empty, "A confirmação da senha não corresponde.");
        if (!ModelState.IsValid) return View();

        try
        {
            var senhaHash = GerenciadorSenhas.CriarHash(senha);
            var uid = await _repositorio.CadastrarPrimeiroAdministradorAsync(
                nome.Trim(), cpfNormalizado, email.Trim(), senhaHash);
            if (!uid.HasValue)
            {
                ModelState.AddModelError(string.Empty,
                    "O primeiro administrador já foi configurado ou o CPF/e-mail informado já está cadastrado. Revise os dados ou volte ao login.");
                return View();
            }

            TempData["PrimeiroAdministradorUid"] = uid.Value.ToString();
            return RedirectToAction(nameof(Login));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            _registro.LogWarning(ex, "CPF ou e-mail duplicado ao cadastrar o primeiro administrador.");
            ModelState.AddModelError(string.Empty, "O CPF ou e-mail informado já está cadastrado. Confira os dados e tente novamente.");
            return View();
        }
        catch (SqlException ex)
        {
            _registro.LogError(ex, "Erro SQL ao cadastrar o primeiro administrador. Código {CodigoSql}, estado {EstadoSql}.", ex.Number, ex.State);
            ModelState.AddModelError(string.Empty, _ambiente.IsDevelopment()
                ? $"Falha SQL {ex.Number} (estado {ex.State}). Confirme conexão, banco selecionado e execução dos scripts de criação."
                : "Não foi possível cadastrar o primeiro administrador. Verifique a conexão e a estrutura do banco.");
            return View();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Sair()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}
