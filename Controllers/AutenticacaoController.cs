using Microsoft.AspNetCore.Mvc;

namespace SIVAD.Controllers
{
    public class AutenticacaoController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult RecuperacaoSenha()
        {
            return View();
        }
    }
}