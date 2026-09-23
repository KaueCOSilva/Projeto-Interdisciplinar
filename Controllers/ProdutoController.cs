using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;

namespace SIVAD.Controllers
{
    public class ProdutoController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Cadastrar()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Cadastrar(Produto produto)
        {
            if (!ModelState.IsValid)
            {
                return View(produto);
            }

            // Persistência será implementada posteriormente.

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Consultar()
        {
            // A consulta será implementada posteriormente.

            return View();
        }

        [HttpGet]
        public IActionResult Detalhes(int id)
        {
            // Busca do produto será implementada posteriormente.

            return View();
        }
    }
}