using Microsoft.AspNetCore.Mvc;
using SIVAD.Repositories;
using SIVAD.Strategies;

namespace SIVAD.Controllers
{
    public class CalculoTotalController : Controller
    {
        private readonly SqlServerRepository _repository;
        private readonly TotalPedidoStrategy _totalPedidoStrategy;
        private readonly TotalCompraEstoqueStrategy _totalCompraEstoqueStrategy;

        public CalculoTotalController(
            SqlServerRepository repository,
            TotalPedidoStrategy totalPedidoStrategy,
            TotalCompraEstoqueStrategy totalCompraEstoqueStrategy)
        {
            _repository = repository;
            _totalPedidoStrategy = totalPedidoStrategy;
            _totalCompraEstoqueStrategy = totalCompraEstoqueStrategy;
        }

        [HttpGet]
        public async Task<IActionResult> CalcularPedido()
        {
            var pedido = await _repository.ObterPrimeiroPedidoAsync();
            if (pedido == null)
            {
                return NotFound("Nenhum pedido encontrado no banco de dados.");
            }

            var total = _totalPedidoStrategy.CalcularTotal(pedido);
            return View(total);
        }

        [HttpGet]
        public async Task<IActionResult> CalcularCompraEstoque()
        {
            var compra = await _repository.ObterPrimeiraCompraEstoqueAsync();
            if (compra == null)
            {
                return NotFound("Nenhuma compra de estoque encontrada no banco de dados.");
            }

            var total = _totalCompraEstoqueStrategy.CalcularTotal(compra);
            return View(total);
        }
    }
}
