using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;

namespace SIVAD.Controllers
{
    public class PedidoController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Registrar(Pedido pedido)
        {
            if (!ModelState.IsValid)
            {
                return View(pedido);
            }

            // Neste ponto, posteriormente:
            // 1. Registrar os itens do pedido.
            // 2. Associar cliente e funcionário.
            // 3. Calcular o total.
            // 4. Salvar no banco.

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Consultar()
        {
            // Consulta dos pedidos será implementada posteriormente.

            return View();
        }

        [HttpGet]
        public IActionResult Detalhes(int id)
        {
            // Busca do pedido será implementada posteriormente.

            return View();
        }
    }
}