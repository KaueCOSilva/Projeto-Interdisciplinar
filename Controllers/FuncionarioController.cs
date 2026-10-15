using Microsoft.AspNetCore.Mvc;
using SIVAD.Models.ViewModels;
using SIVAD.Repositories;

namespace SIVAD.Controllers;

public class FuncionarioController : Controller
{
    private readonly ISIVADRepository _repository;
    public FuncionarioController(ISIVADRepository repository) => _repository = repository;

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(RegistrarPedido));

    [HttpGet]
    public async Task<IActionResult> RegistrarPedido()
    {
        var model = new EmployeeOrderViewModel
        {
            Produtos = await _repository.ListarProdutosAsync(somenteComEstoque: true),
            Funcionarios = await _repository.ListarFuncionariosAsync()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ResumoCompra(int? id)
    {
        if (id is null) return RedirectToAction(nameof(RegistrarPedido));
        var pedido = await _repository.ObterPedidoAsync(id.Value);
        if (pedido is null) return NotFound();
        return View(pedido);
    }
}
