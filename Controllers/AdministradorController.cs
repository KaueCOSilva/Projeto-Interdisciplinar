using Microsoft.AspNetCore.Mvc;
using SIVAD.Models.ViewModels;
using SIVAD.Repositories;

namespace SIVAD.Controllers;

public class AdministradorController : Controller
{
    private readonly ISIVADRepository _repository;

    private async Task DefinirContagemAlertasAsync()
    {
        ViewBag.AlertasEstoque = (await _repository.ListarProdutosEstoqueBaixoAsync()).Count;
    }

    public AdministradorController(ISIVADRepository repository) => _repository = repository;

    [HttpGet]
    public async Task<IActionResult> Dashboard() => View(await _repository.ObterDashboardAsync());

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(Dashboard));

    [HttpGet]
    public async Task<IActionResult> Relatorios(string periodo = "mensais")
    {
        ViewBag.Periodo = periodo;
        await DefinirContagemAlertasAsync();
        return View(new ReportViewModel { Movimentos = await _repository.ListarMovimentosRelatorioAsync() });
    }

    [HttpGet]
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
        ViewBag.AlertasEstoque = produtos.Count;
        return View(produtos);
    }
    [HttpGet]
    public async Task<IActionResult> CadastrarUsuario()
    {
        await DefinirContagemAlertasAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CadastrarUsuario(string nome)
    {
        try
        {
            var id = await _repository.CadastrarFuncionarioAsync(nome);
            TempData["Sucesso"] = $"Funcionário cadastrado no banco (ID {id}).";
            return RedirectToAction(nameof(GerenciarUsuarios));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Erro"] = ex.Message;
            return RedirectToAction(nameof(CadastrarUsuario));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GerenciarUsuarios(int? editarId)
    {
        await DefinirContagemAlertasAsync();
        var funcionarios = await _repository.ListarFuncionariosAsync();
        var funcionario = editarId.HasValue
            ? funcionarios.FirstOrDefault(item => item.Id == editarId.Value)
            : null;

        if (editarId.HasValue && funcionario is null)
            TempData["Erro"] = "Funcionário não encontrado ou inativo.";

        return View(new EmployeeManagementViewModel
        {
            Funcionarios = funcionarios,
            FuncionarioEmEdicaoId = funcionario?.Id,
            FuncionarioEmEdicaoNome = funcionario?.Nome
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarFuncionario(int id, string nome)
    {
        try
        {
            if (await _repository.AtualizarFuncionarioAsync(id, nome)) TempData["Sucesso"] = "Nome do funcionário atualizado.";
            else TempData["Erro"] = "Funcionário não encontrado ou já removido.";
        }
        catch (InvalidOperationException ex) { TempData["Erro"] = ex.Message; }
        return RedirectToAction(nameof(GerenciarUsuarios));
    }

    [HttpPost]
    public IActionResult AlterarSenha(string uid, string nomeUsuario, string novaSenha, string confirmarSenha)
    {
        TempData["Erro"] = "O schema atual ainda não possui tabela de contas/autenticação.";
        return RedirectToAction(nameof(GerenciarUsuarios));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
