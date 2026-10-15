using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;
using SIVAD.Repositories;
using SIVAD.Strategies;

namespace SIVAD.Controllers;

public class CompraEstoqueController : Controller
{
    private readonly ISIVADRepository _repository;
    private readonly TotalCompraEstoqueStrategy _totalStrategy;

    public CompraEstoqueController(ISIVADRepository repository, TotalCompraEstoqueStrategy totalStrategy)
    {
        _repository = repository;
        _totalStrategy = totalStrategy;
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction("MovimentoCompras", "Administrador");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(CompraEstoque compraEstoque)
    {
        compraEstoque.Itens = compraEstoque.Itens.Where(item => item.Quantidade > 0).ToList();
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
}
