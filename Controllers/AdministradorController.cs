using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
using SIVAD.Models.ViewModels;
using SIVAD.Repositories;
using SIVAD.Filtros;
using SIVAD.Seguranca;

namespace SIVAD.Controllers;

[ExigirSessao("Administrador")]
public class AdministradorController : Controller
{
    private readonly IRepositorioSIVAD _repository;

    private async Task DefinirContagemAlertasAsync()
    {
        var produtos = (await _repository.ListarProdutosEstoqueBaixoAsync()).Count;
        var recuperacoes = (await _repository.ListarSolicitacoesRecuperacaoSenhaAsync()).Count;
        ViewBag.TotalNotificacoes = produtos + recuperacoes;
    }

    public AdministradorController(IRepositorioSIVAD repository) => _repository = repository;

    [HttpGet]
    // READ: carrega indicadores agregados para o painel administrativo.
    public async Task<IActionResult> Dashboard()
    {
        await DefinirContagemAlertasAsync();
        return View(await _repository.ObterDashboardAsync());
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(Dashboard));

    [HttpGet]
    // READ: consulta movimentos consolidados para os relatórios.
    public async Task<IActionResult> Relatorios(string periodo = "mensais")
    {
        ViewBag.Periodo = periodo;
        await DefinirContagemAlertasAsync();
        return View(new ReportViewModel { Movimentos = await _repository.ListarMovimentosRelatorioAsync() });
    }

    [HttpGet]
    // READ: carrega produtos e categorias para a tela de estoque.
    public async Task<IActionResult> Estoque()
    {
        await DefinirContagemAlertasAsync();
        return View(new SIVAD.Models.ViewModels.StockViewModel
        {
            Produtos = await _repository.ListarProdutosAsync(),
            Categorias = await _repository.ListarCategoriasAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> MovimentoCompras()
    {
        await DefinirContagemAlertasAsync();
        var model = new StockPurchaseViewModel
        {
            Produtos = await _repository.ListarProdutosAsync(),
            Administradores = await _repository.ListarAdministradoresAsync(),
            HistoricoCompras = await _repository.ListarComprasAsync()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Notificacoes()
    {
        var produtos = await _repository.ListarProdutosEstoqueBaixoAsync();
        var recuperacoes = await _repository.ListarSolicitacoesRecuperacaoSenhaAsync();
        var notificacoes = new CentralNotificacoesViewModel
        {
            ProdutosEstoqueBaixo = produtos,
            RecuperacoesPendentes = recuperacoes
        };
        ViewBag.TotalNotificacoes = notificacoes.TotalNotificacoes;
        return View(notificacoes);
    }
    [HttpGet]
    public async Task<IActionResult> CadastrarUsuario()
    {
        await DefinirContagemAlertasAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // CREATE: grava Pessoa e Funcionário dentro de uma transação no Repository.
    public async Task<IActionResult> CadastrarUsuario(string nome, string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email) ||
            string.IsNullOrWhiteSpace(senha) || senha.Length < 8)
        {
            TempData["Erro"] = "Informe um e-mail válido e uma senha com pelo menos 8 caracteres.";
            return RedirectToAction(nameof(CadastrarUsuario));
        }

        try
        {
            var hash = GerenciadorSenhas.CriarHash(senha);
            var id = await _repository.CadastrarFuncionarioAsync(nome, email, hash);
            TempData["Sucesso"] = $"Funcionário cadastrado (UID {id}); o acesso foi criado.";
            return RedirectToAction(nameof(GerenciarUsuarios));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Erro"] = ex.Message;
            return RedirectToAction(nameof(CadastrarUsuario));
        }
    }

    [HttpGet]
    // READ: lista apenas funcionários ativos e prepara o item em edição.
    public async Task<IActionResult> GerenciarUsuarios(int? editarId, int? solicitacaoId)
    {
        await DefinirContagemAlertasAsync();
        var funcionarios = await _repository.ListarFuncionariosAsync();
        var funcionario = editarId.HasValue
            ? funcionarios.FirstOrDefault(item => item.Id == editarId.Value)
            : null;
        int? solicitacaoValida = null;

        if (solicitacaoId.HasValue)
        {
            var solicitacao = (await _repository.ListarSolicitacoesRecuperacaoSenhaAsync())
                .FirstOrDefault(item => item.SolicitacaoId == solicitacaoId.Value);
            if (solicitacao is null || funcionario?.Id != solicitacao.PessoaId)
                TempData["Erro"] = "A solicitação pendente não corresponde ao funcionário selecionado.";
            else
            {
                solicitacaoValida = solicitacao.SolicitacaoId;
                TempData["Aviso"] = $"Informe uma nova senha para {solicitacao.Nome} e clique em Salvar alterações para concluir a solicitação.";
            }
        }

        if (editarId.HasValue && funcionario is null)
            TempData["Erro"] = "Funcionário não encontrado ou inativo.";

        return View(new EmployeeManagementViewModel
        {
            Funcionarios = funcionarios,
            FuncionarioEmEdicaoId = funcionario?.Id,
            FuncionarioEmEdicaoNome = funcionario?.Nome,
            SolicitacaoRecuperacaoId = solicitacaoValida
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // UPDATE: altera dados do funcionário e finaliza a recuperação na mesma gravação.
    public async Task<IActionResult> EditarFuncionario(int id, string nome, string novaSenha, int? solicitacaoId)
    {
        if (string.IsNullOrWhiteSpace(novaSenha) && solicitacaoId.HasValue)
        {
            TempData["Erro"] = "Informe a nova senha para concluir a recuperação.";
            return RedirectToAction(nameof(GerenciarUsuarios), new { editarId = id, solicitacaoId });
        }
        if (!string.IsNullOrWhiteSpace(novaSenha) && novaSenha.Length < 8)
        {
            TempData["Erro"] = "A nova senha deve ter pelo menos 8 caracteres.";
            return RedirectToAction(nameof(GerenciarUsuarios), new { editarId = id, solicitacaoId });
        }

        try
        {
            var senhaHash = string.IsNullOrWhiteSpace(novaSenha) ? null : GerenciadorSenhas.CriarHash(novaSenha);
            if (await _repository.AtualizarFuncionarioAsync(id, nome, senhaHash, solicitacaoId))
                TempData["Sucesso"] = solicitacaoId.HasValue
                    ? "Senha atualizada e solicitação de recuperação finalizada."
                    : "Dados do funcionário atualizados.";
            else
            {
                TempData["Erro"] = "Funcionário ou solicitação pendente não encontrada; nenhuma alteração foi salva.";
                return RedirectToAction(nameof(GerenciarUsuarios), new { editarId = id, solicitacaoId });
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["Erro"] = ex.Message;
            return RedirectToAction(nameof(GerenciarUsuarios), new { editarId = id, solicitacaoId });
        }
        catch (SqlException)
        {
            TempData["Erro"] = "Não foi possível salvar as alterações no banco de dados.";
            return RedirectToAction(nameof(GerenciarUsuarios), new { editarId = id, solicitacaoId });
        }
        return RedirectToAction(nameof(GerenciarUsuarios));
    }

    [HttpGet]
    public async Task<IActionResult> ConfigurarAcessoFuncionario(int id)
    {
        var funcionario = (await _repository.ListarFuncionariosAsync()).FirstOrDefault(item => item.Id == id);
        if (funcionario is null) return NotFound();
        ViewBag.FuncionarioId = funcionario.Id;
        ViewBag.FuncionarioNome = funcionario.Nome;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfigurarAcessoFuncionario(int id, string email, string senha, string confirmarSenha)
    {
        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email) ||
            string.IsNullOrWhiteSpace(senha) || senha.Length < 8 || senha != confirmarSenha)
        {
            TempData["Erro"] = "Informe e-mail válido, senha com pelo menos 8 caracteres e confirmação igual.";
            return RedirectToAction(nameof(ConfigurarAcessoFuncionario), new { id });
        }

        try
        {
            var hash = GerenciadorSenhas.CriarHash(senha);
            if (await _repository.ConfigurarAcessoFuncionarioAsync(id, email, hash))
                TempData["Sucesso"] = $"Acesso do funcionário UID {id} foi configurado.";
            else
                TempData["Erro"] = "Funcionário ativo não encontrado ou UID associado a outro perfil.";
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            TempData["Erro"] = "Esse e-mail já está associado a outra conta.";
        }
        return RedirectToAction(nameof(GerenciarUsuarios));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Aprovar apenas encaminha à edição; a solicitação segue pendente até salvar a nova senha.
    public async Task<IActionResult> AprovarSolicitacaoRecuperacaoSenha(int solicitacaoId)
    {
        try
        {
            var solicitacao = (await _repository.ListarSolicitacoesRecuperacaoSenhaAsync())
                .FirstOrDefault(item => item.SolicitacaoId == solicitacaoId);
            if (solicitacao is null)
            {
                TempData["Erro"] = "Solicitação pendente não encontrada.";
                return RedirectToAction(nameof(Notificacoes));
            }

            return RedirectToAction(nameof(GerenciarUsuarios), new
            {
                editarId = solicitacao.PessoaId,
                solicitacaoId = solicitacao.SolicitacaoId
            });
        }
        catch (SqlException)
        {
            TempData["Erro"] = "Não foi possível abrir a solicitação; confira a conexão com o banco.";
            return RedirectToAction(nameof(Notificacoes));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejeitarSolicitacaoRecuperacaoSenha(int solicitacaoId)
    {
        try
        {
            if (await _repository.RejeitarSolicitacaoRecuperacaoSenhaAsync(solicitacaoId))
                TempData["Sucesso"] = "Solicitação rejeitada. Se necessário, o usuário poderá fazer um novo pedido.";
            else
                TempData["Erro"] = "Solicitação pendente não encontrada.";
        }
        catch (SqlException)
        {
            TempData["Erro"] = "Não foi possível rejeitar a solicitação; confira a conexão com o banco.";
        }
        return RedirectToAction(nameof(Notificacoes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // DELETE lógico: desativa o funcionário e preserva pedidos históricos.
    public async Task<IActionResult> ExcluirUsuario(int id)
    {
        if (id <= 0) return BadRequest();
        if (await _repository.DesativarFuncionarioAsync(id))
            TempData["Sucesso"] = "Funcionário removido da lista; pedidos históricos foram preservados.";
        else
            TempData["Erro"] = "Funcionário não encontrado ou já removido.";
        return RedirectToAction(nameof(GerenciarUsuarios));
    }
}
