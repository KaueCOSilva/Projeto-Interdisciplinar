using Microsoft.AspNetCore.Mvc;
using SIVAD.Repositories;
using SIVAD.Models;
using SIVAD.Strategies;

namespace SIVAD.Controllers
{
    // Controlador de Vendas (Nível 3): cria o pedido e calcula o total usando a Strategy.
    // Não possui views próprias: as telas são as de Views/Funcionario.
    public class PedidoController : Controller
    {
        private const int StatusPendente = 0;
        private const int StatusFinalizado = 1;
        private const int StatusCancelado = 2;

        private readonly SqlServerRepository _repository;
        private readonly TotalPedidoStrategy _totalPedidoStrategy;

        public PedidoController(
            SqlServerRepository repository,
            TotalPedidoStrategy totalPedidoStrategy)
        {
            _repository = repository;
            _totalPedidoStrategy = totalPedidoStrategy;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("RegistrarPedido", "Funcionario");
        }

        [HttpPost]
        public async Task<IActionResult> Registrar(Pedido pedido)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Funcionario/RegistrarPedido.cshtml", pedido);
            }

            // A associação do cliente e do funcionário logado ainda faz parte do protótipo.
            pedido.ValorTotal = _totalPedidoStrategy.CalcularTotal(pedido);
            pedido.Status = StatusPendente;

            await _repository.RegistrarPedidoAsync(pedido);

            return RedirectToAction("ResumoCompra", "Funcionario", new { id = pedido.Codigo });
        }

        [HttpPost]
        public async Task<IActionResult> Finalizar(int id)
        {
            var atualizado = await _repository.AtualizarStatusPedidoAsync(id, StatusFinalizado);
            if (!atualizado)
            {
                return NotFound();
            }

            // A baixa de estoque ainda não faz parte deste protótipo.
            TempData["Sucesso"] = "Compra contabilizada.";
            return RedirectToAction("RegistrarPedido", "Funcionario");
        }

        [HttpPost]
        public async Task<IActionResult> Cancelar(int id)
        {
            var atualizado = await _repository.AtualizarStatusPedidoAsync(id, StatusCancelado);
            if (!atualizado)
            {
                return NotFound();
            }

            // A devolução dos itens ao estoque ainda não faz parte deste protótipo.
            TempData["Sucesso"] = "Venda cancelada.";
            return RedirectToAction("Relatorios", "Administrador");
        }
    }
}
