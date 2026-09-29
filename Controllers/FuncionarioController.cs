using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SIVAD.Data;

namespace SIVAD.Controllers
{
    // Telas do funcionário (documento 2.4.4):
    // "Registro de Compras" (RegistrarPedido) e "Resumo de Compra" (ResumoCompra).
    //
    // Este controller só entrega as páginas. Os formulários enviam para o PedidoController:
    //   - Finalizar Compra      -> POST Pedido/Registrar  (calcula o total e vai para o resumo)
    //   - Contabilizar Compra   -> POST Pedido/Finalizar  (conclui e volta para o registro)
    public class FuncionarioController : Controller
    {
        private readonly AppDbContext _context;

        public FuncionarioController(AppDbContext context)
        {
            _context = context;
        }

        // A tela inicial do funcionário é o registro de compra.
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("RegistrarPedido");
        }

        [HttpGet]
        public IActionResult RegistrarPedido()
        {
            return View();
        }

        // Sem id (clique direto no menu "Resumo de Compra") não há pedido para resumir.
        [HttpGet]
        public async Task<IActionResult> ResumoCompra(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("RegistrarPedido");
            }

            var pedido = await _context.Pedidos.FindAsync(id.Value);

            if (pedido == null)
            {
                return NotFound();
            }

            await _context.Entry(pedido).Collection(p => p.Itens).LoadAsync();

            return View(pedido);
        }
    }
}
