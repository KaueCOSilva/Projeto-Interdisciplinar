using Microsoft.AspNetCore.Mvc;
using SIVAD.Repositories;
using SIVAD.Strategies;

namespace SIVAD.Controllers
{
    public class CalculoTotalController : Controller
    {
        private readonly ISIVADRepository _repository;
        private readonly TotalPedidoStrategy _totalPedidoStrategy;
        private readonly TotalCompraEstoqueStrategy _totalCompraEstoqueStrategy;

        public CalculoTotalController(
            ISIVADRepository repository,
            TotalPedidoStrategy totalPedidoStrategy,
            TotalCompraEstoqueStrategy totalCompraEstoqueStrategy)
        {
            _repository = repository;
            _totalPedidoStrategy = totalPedidoStrategy;
            _totalCompraEstoqueStrategy = totalCompraEstoqueStrategy;
        }

        [HttpGet]
        // READ + cálculo: busca o primeiro pedido e delega a soma à Strategy específica.
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
        // READ + cálculo: busca a primeira compra e soma seus itens pela Strategy de estoque.
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
