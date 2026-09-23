using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;
using SIVAD.Strategies;

namespace SIVAD.Controllers
{
    public class CalculoTotalController : Controller
    {
        private readonly TotalPedidoStrategy _totalPedidoStrategy;
        private readonly TotalCompraEstoqueStrategy _totalCompraEstoqueStrategy;

        public CalculoTotalController(
            TotalPedidoStrategy totalPedidoStrategy,
            TotalCompraEstoqueStrategy totalCompraEstoqueStrategy)
        {
            _totalPedidoStrategy = totalPedidoStrategy;
            _totalCompraEstoqueStrategy = totalCompraEstoqueStrategy;
        }

        [HttpGet]
        public IActionResult CalcularPedido(Pedido pedido)
        {
            float total = _totalPedidoStrategy.CalcularTotal(pedido);

            return View(total);
        }

        [HttpGet]
        public IActionResult CalcularCompraEstoque(CompraEstoque compraEstoque)
        {
            float total = _totalCompraEstoqueStrategy.CalcularTotal(compraEstoque);

            return View(total);
        }
    }
}