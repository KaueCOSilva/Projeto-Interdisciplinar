using Microsoft.AspNetCore.Mvc;
using SIVAD.Repositories;

namespace SIVAD.Controllers
{
    // Telas do funcionário: registro e resumo de compra.
    public class FuncionarioController : Controller
    {
        private readonly SqlServerRepository _repository;

        public FuncionarioController(SqlServerRepository repository)
        {
            _repository = repository;
        }

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

        [HttpGet]
        public async Task<IActionResult> ResumoCompra(int? id)
        {
            if (id == null)
            {
                // A tela contém dados demonstrativos enquanto o fluxo completo é desenvolvido.
                return View();
            }

            var pedido = await _repository.ObterPedidoAsync(id.Value);
            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
        }
    }
}
