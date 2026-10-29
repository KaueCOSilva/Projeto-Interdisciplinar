using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;
using SIVAD.Models.ViewModels;
using SIVAD.Repositories;
using SIVAD.Strategies;

namespace SIVAD.Controllers;

public class CompraEstoqueController : Controller
{
    private readonly IRepositorioSIVAD _repository;
    private readonly TotalCompraEstoqueStrategy _totalStrategy;

    public CompraEstoqueController(IRepositorioSIVAD repository, TotalCompraEstoqueStrategy totalStrategy)
    {
        _repository = repository;
        _totalStrategy = totalStrategy;
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction("MovimentoCompras", "Administrador");

    [HttpPost]
    [ValidateAntiForgeryToken]
    // CREATE: grava compra + itens e aumenta o estoque dentro de transação.
    public async Task<IActionResult> Registrar(CompraEstoque compraEstoque)
    {
        compraEstoque.Itens = (compraEstoque.Itens ?? []).Where(item => item.Quantidade > 0).ToList();
        if (compraEstoque.AdministradorId <= 0 || string.IsNullOrWhiteSpace(compraEstoque.FornecedorNome) || compraEstoque.Itens.Count == 0)
        {
            TempData["Erro"] = "Informe o nome do fornecedor, o administrador e ao menos um produto com quantidade positiva.";
            return RedirectToAction("MovimentoCompras", "Administrador");
        }

        compraEstoque.FornecedorNome = compraEstoque.FornecedorNome.Trim();
        if (compraEstoque.FornecedorNome.Length > 150)
        {
            TempData["Erro"] = "O nome do fornecedor deve ter até 150 caracteres.";
            return RedirectToAction("MovimentoCompras", "Administrador");
        }

        foreach (var item in compraEstoque.Itens)
            item.Valor = item.PrecoUnitario * item.Quantidade;
        compraEstoque.Total = _totalStrategy.CalcularTotal(compraEstoque);
        try
        {
            await _repository.RegistrarCompraEstoqueAsync(compraEstoque);
            TempData["Sucesso"] = $"Compra #{compraEstoque.Numero} registrada; quantidades do estoque atualizadas.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction("MovimentoCompras", "Administrador");
    }

    [HttpGet]
    // READ de edição: busca a compra e monta o formulário com dados atuais.
    public async Task<IActionResult> Editar(int numero)
    {
        if (numero <= 0) return BadRequest();
        var compra = await _repository.ObterCompraEstoqueAsync(numero);
        if (compra is null)
        {
            TempData["Erro"] = "Compra não encontrada.";
            return RedirectToAction("MovimentoCompras", "Administrador");
        }
        if (compra.Status != 1)
        {
            TempData["Erro"] = "Somente compras recebidas podem ser editadas.";
            return RedirectToAction("MovimentoCompras", "Administrador");
        }

        ViewBag.AlertasEstoque = (await _repository.ListarProdutosEstoqueBaixoAsync()).Count;
        var model = new StockPurchaseViewModel
        {
            Produtos = await _repository.ListarProdutosAsync(incluirInativos: true),
            Administradores = await _repository.ListarAdministradoresAsync(),
            HistoricoCompras = await _repository.ListarComprasAsync(),
            CompraEmEdicao = compra
        };
        return View("~/Views/Administrador/MovimentoCompras.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // UPDATE: recompõe o estoque antigo antes de aplicar os novos itens.
    public async Task<IActionResult> Editar(CompraEstoque compraEstoque)
    {
        compraEstoque.Itens = (compraEstoque.Itens ?? []).Where(item => item.Quantidade > 0).ToList();
        compraEstoque.FornecedorNome = (compraEstoque.FornecedorNome ?? string.Empty).Trim();
        if (compraEstoque.Numero <= 0 || compraEstoque.FornecedorNome.Length == 0 || compraEstoque.FornecedorNome.Length > 150 || compraEstoque.Itens.Count == 0)
        {
            TempData["Erro"] = "Informe fornecedor e mantenha ao menos um item com quantidade positiva.";
            return RedirectToAction(nameof(Editar), new { numero = compraEstoque.Numero });
        }

        foreach (var item in compraEstoque.Itens)
            item.Valor = decimal.Round(item.PrecoUnitario * item.Quantidade, 2, MidpointRounding.AwayFromZero);
        compraEstoque.Total = _totalStrategy.CalcularTotal(compraEstoque);
        try
        {
            if (!await _repository.AtualizarCompraEstoqueAsync(compraEstoque))
                TempData["Erro"] = "A compra não foi encontrada ou já está cancelada.";
            else
                TempData["Sucesso"] = $"Compra #{compraEstoque.Numero} atualizada; estoque, relatórios e gráficos recalculados.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Erro"] = ex.Message;
            return RedirectToAction(nameof(Editar), new { numero = compraEstoque.Numero });
        }
        return RedirectToAction("MovimentoCompras", "Administrador");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // DELETE de negócio: cancela a entrada e reverte suas quantidades no estoque.
    public async Task<IActionResult> Cancelar(int numero)
    {
        try
        {
            if (!await _repository.CancelarCompraEstoqueAsync(numero))
                TempData["Erro"] = "Compra não encontrada ou já cancelada.";
            else
                TempData["Sucesso"] = $"Compra #{numero} cancelada; quantidades retiradas do estoque e movimento excluído dos relatórios e gráficos.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction("MovimentoCompras", "Administrador");
    }
}
