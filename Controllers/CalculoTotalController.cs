using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIVAD.Data;
using SIVAD.Strategies;

namespace SIVAD.Controllers
{
    public class CalculoTotalController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TotalPedidoStrategy _totalPedidoStrategy;
        private readonly TotalCompraEstoqueStrategy _totalCompraEstoqueStrategy;

        public CalculoTotalController(
            AppDbContext context,
            TotalPedidoStrategy totalPedidoStrategy,
            TotalCompraEstoqueStrategy totalCompraEstoqueStrategy)
        {
            _context = context;
            _totalPedidoStrategy = totalPedidoStrategy;
            _totalCompraEstoqueStrategy = totalCompraEstoqueStrategy;
        }

        // Acessível em: /CalculoTotal/CalcularPedido
        [HttpGet]
        public async Task<IActionResult> CalcularPedido()
        {
            // Busca o primeiro pedido cadastrado no SQL Server, trazendo seus itens
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync();

            if (pedido == null)
            {
                return NotFound("Nenhum pedido encontrado no banco de dados.");
            }

            float total = _totalPedidoStrategy.CalcularTotal(pedido);

            return View(total);
        }

        // Acessível em: /CalculoTotal/CalcularCompraEstoque
        [HttpGet]
        public async Task<IActionResult> CalcularCompraEstoque()
        {
            // Busca a primeira compra de estoque cadastrada no SQL Server, trazendo seus itens
            var compra = await _context.CompraEstoque
                .Include(c => c.Itens)
                .FirstOrDefaultAsync();

            if (compra == null)
            {
                return NotFound("Nenhuma compra de estoque encontrada no banco de dados.");
            }

            float total = _totalCompraEstoqueStrategy.CalcularTotal(compra);

            return View(total);
        }
    }
}