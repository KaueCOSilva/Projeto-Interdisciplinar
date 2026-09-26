using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;

namespace SIVAD.Controllers
{
    public class AdministradorController : Controller
    {
        // =====================================================
        // DASHBOARD
        // =====================================================

        [HttpGet]
        public IActionResult Dashboard()
        {
            return View();
        }


        // =====================================================
        // INDEX
        // =====================================================

        // Verificar com o professor sobre o uso do Index. Manter assim? 
        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }


        // =====================================================
        // LOGIN
        // =====================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string senha)
        {
            // A autenticação será implementada posteriormente.

            if (string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(senha))
            {
                ViewBag.Mensagem =
                    "Informe o e-mail e a senha.";

                return View();
            }

            return RedirectToAction("Dashboard");
        }


        // =====================================================
        // CADASTRO DE USUÁRIO
        // =====================================================

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

            return RedirectToAction("Dashboard");
        }


        // =====================================================
        // EXCLUIR USUÁRIO
        // =====================================================

        [HttpPost]
        public IActionResult ExcluirUsuario(int id)
        {
            // A exclusão será implementada posteriormente.

            return RedirectToAction("Dashboard");
        }


        // =====================================================
        // CADASTRO DE PRODUTO
        // =====================================================

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

            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public IActionResult Relatorios()
        {
            return View();
        }
    }
}