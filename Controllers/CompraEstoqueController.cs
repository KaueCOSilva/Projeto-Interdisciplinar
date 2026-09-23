using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;

namespace SIVAD.Controllers
{
    public class CompraEstoqueController : Controller
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
        public IActionResult Registrar(CompraEstoque compraEstoque)
        {
            if (!ModelState.IsValid)
            {
                return View(compraEstoque);
            }

            // Posteriormente:
            // 1. Registrar fornecedor.
            // 2. Registrar os itens da compra.
            // 3. Calcular o total.
            // 4. Atualizar o estoque.
            // 5. Salvar no banco.

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Consultar()
        {
            // Consulta será implementada posteriormente.

            return View();
        }

        [HttpGet]
        public IActionResult Detalhes(int id)
        {
            // Busca da compra será implementada posteriormente.

            return View();
        }
    }
}