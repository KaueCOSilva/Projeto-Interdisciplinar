using Microsoft.AspNetCore.Mvc;
using SIVAD.Models.ViewModels;
using SIVAD.Repositories;
using SIVAD.Filtros;

namespace SIVAD.Controllers;

[ExigirSessao("Funcionario")]
public class FuncionarioController : Controller
{
    private readonly IRepositorioSIVAD _repository;
    public FuncionarioController(IRepositorioSIVAD repository) => _repository = repository;

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(RegistrarPedido));

    [HttpGet]
    // READ: carrega produtos ativos e vincula o pedido ao funcionário autenticado.
    public async Task<IActionResult> RegistrarPedido()
    {
        var funcionarioId = HttpContext.Session.GetInt32("id");
        if (funcionarioId is null) return Unauthorized();

        var funcionarios = await _repository.ListarFuncionariosAsync();
        var model = new PedidoFuncionarioViewModel
        {
            Produtos = await _repository.ListarProdutosAsync(somenteComEstoque: true),
            Funcionarios = funcionarios.Where(funcionario => funcionario.Id == funcionarioId.Value).ToList()
        };
        return View(model);
    }

    [HttpGet]
    // READ: busca somente um pedido pendente pertencente ao funcionário autenticado.
    public async Task<IActionResult> ResumoCompra(int? id)
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        var funcionarioId = HttpContext.Session.GetInt32("id");
        if (funcionarioId is null) return Unauthorized();

        if (id is null)
        {
            var ultimoPedido = await _repository.ObterUltimoPedidoPendenteAsync(funcionarioId.Value);
            if (ultimoPedido is null)
            {
                TempData["Sucesso"] = "Não há pedido pendente no resumo. O histórico contabilizado foi preservado; você pode registrar um novo pedido.";
                return RedirectToAction(nameof(RegistrarPedido));
            }

            return View(ultimoPedido);
        }

        var pedido = await _repository.ObterPedidoAsync(id.Value);
        if (pedido is null || pedido.FuncionarioId != funcionarioId.Value) return NotFound();
        if (pedido.Status != 0)
        {
            TempData["Sucesso"] = "Este pedido já foi contabilizado ou cancelado. O registro foi preservado no histórico.";
            return RedirectToAction(nameof(RegistrarPedido));
        }
        return View(pedido);
    }
}
