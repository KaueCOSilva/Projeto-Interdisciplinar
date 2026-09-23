using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;

namespace SIVAD.Controllers
{
    public class AdministradorController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string senha)
        {
            // A autenticação será implementada posteriormente.

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
            {
                ViewBag.Mensagem = "Informe o e-mail e a senha.";
                return View();
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult CadastrarUsuario()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CadastrarUsuario(Pessoa pessoa)
        {
            // O cadastro será integrado ao banco posteriormente.

            if (!ModelState.IsValid)
            {
                return View(pessoa);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ExcluirUsuario(int id)
        {
            // A exclusão será implementada posteriormente.

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult CadastrarProduto()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CadastrarProduto(Produto produto)
        {
            // O cadastro será integrado ao banco posteriormente.

            if (!ModelState.IsValid)
            {
                return View(produto);
            }

            return RedirectToAction("Index");
        }
    }
}