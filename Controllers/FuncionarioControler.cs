using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;

namespace SIVAD.Controllers
{
    public class FuncionarioController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ConsultarEstoque()
        {
            // A consulta será integrada ao banco posteriormente.

            return View();
        }

        [HttpGet]
        public IActionResult RegistrarPedido()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RegistrarPedido(Pedido pedido)
        {
            // O registro será integrado ao banco posteriormente.

            if (!ModelState.IsValid)
            {
                return View(pedido);
            }

            return RedirectToAction("Index");
        }
    }
}