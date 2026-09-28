using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SIVAD.Data;
using SIVAD.Models;
using SIVAD.Strategies;

namespace SIVAD.Controllers
{
    // Controlador de Vendas (Nível 3): cria o pedido e calcula o total usando a Strategy.
    // Não possui views próprias: as telas são as de Views/Funcionario.
    public class PedidoController : Controller
    {
        // Ajuste se o seu projeto usar outros valores para Pedido.Status.
        private const int StatusPendente = 0;
        private const int StatusFinalizado = 1;
        private const int StatusCancelado = 2;

        private readonly AppDbContext _context;
        private readonly TotalPedidoStrategy _totalPedidoStrategy;

        public PedidoController(AppDbContext context, TotalPedidoStrategy totalPedidoStrategy)
        {
            _context = context;
            _totalPedidoStrategy = totalPedidoStrategy;
        }

        // Não existe Views/Pedido/Index.cshtml.
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("RegistrarPedido", "Funcionario");
        }

        // Botão "Finalizar Compra" da tela Registro de Compras.
        [HttpPost]
        public async Task<IActionResult> Registrar(Pedido pedido)
        {
            if (!ModelState.IsValid)
            {
                // A view está em outra pasta (Views/Funcionario), então o caminho é explícito.
                return View("~/Views/Funcionario/RegistrarPedido.cshtml", pedido);
            }

            // Posteriormente:
            // - Ler a NF-e pelo conector de código de barras (botão "Ler Código de NF-e").
            // - Associar cliente (CPF/nome) e funcionário logado.

            pedido.ValorTotal = (decimal)_totalPedidoStrategy.CalcularTotal(pedido);
            pedido.Status = StatusPendente;

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Segue para o Resumo de Compra, não volta para o Index.
            return RedirectToAction("ResumoCompra", "Funcionario", new { id = pedido.Codigo });
        }

        // Botão "Contabilizar Compra" da tela Resumo de Compra.
        [HttpPost]
        public async Task<IActionResult> Finalizar(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido == null)
            {
                return NotFound();
            }

            pedido.Status = StatusFinalizado;

            // Posteriormente: dar baixa no estoque dos produtos dos itens do pedido.

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Compra contabilizada.";
            return RedirectToAction("RegistrarPedido", "Funcionario");
        }

        // Caso de uso do administrador: "Estornar/Cancelar Venda".
        [HttpPost]
        public async Task<IActionResult> Cancelar(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido == null)
            {
                return NotFound();
            }

            pedido.Status = StatusCancelado;

            // Posteriormente: devolver os itens ao estoque.

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Venda cancelada.";
            return RedirectToAction("Relatorios", "Administrador");
        }
    }
}
