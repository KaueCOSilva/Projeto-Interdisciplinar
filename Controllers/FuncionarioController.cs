using Microsoft.AspNetCore.Mvc;
using SIVAD.Models.ViewModels;
using SIVAD.Repositories;

namespace SIVAD.Controllers;

public class FuncionarioController : Controller
{
    private readonly IRepositorioSIVAD _repository;
    public FuncionarioController(IRepositorioSIVAD repository) => _repository = repository;

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(RegistrarPedido));

    [HttpGet]
    // READ: carrega produtos ativos com saldo e funcionários para montar um novo pedido.
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
    // READ: busca um pedido pendente e seus itens para a tela de resumo.
    public async Task<IActionResult> ResumoCompra(int? id)
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        if (id is null)
        {
            var ultimoPedido = await _repository.ObterUltimoPedidoPendenteAsync();
            if (ultimoPedido is null)
            {
                TempData["Sucesso"] = "Não há pedido pendente no resumo. O histórico contabilizado foi preservado; você pode registrar um novo pedido.";
                return RedirectToAction(nameof(RegistrarPedido));
            }

            return View(ultimoPedido);
        }

        var pedido = await _repository.ObterPedidoAsync(id.Value);
        if (pedido is null) return NotFound();
        if (pedido.Status != 0)
        {
            TempData["Sucesso"] = "Este pedido já foi contabilizado ou cancelado. O registro foi preservado no histórico.";
            return RedirectToAction(nameof(RegistrarPedido));
        }
        return View(pedido);
    }
}
