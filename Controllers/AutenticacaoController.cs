using Microsoft.AspNetCore.Mvc;

namespace SIVAD.Controllers
{
    // Telas: Login (2.4.1) e Esqueci minha senha (2.4.2).
    // Os campos dos formulários devem ter name="uid", "senha", "nomeUsuario" e "email".
    public class AutenticacaoController : Controller
    {
        // =====================================================
        // LOGIN
        // =====================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string uid, string senha)
        {
            if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(senha))
            {
                ModelState.AddModelError(string.Empty, "Informe o UID e a senha.");
                return View();
            }

            string? perfil = ValidarCredenciais(uid, senha);

            if (perfil == null)
            {
                ModelState.AddModelError(string.Empty, "UID ou senha inválidos.");
                return View();
            }

            // BPMN: "Selecionar perfil" define o destino após o login.
            return perfil switch
            {
                "Administrador" => RedirectToAction("Dashboard", "Administrador"),
                "Funcionario" => RedirectToAction("RegistrarPedido", "Funcionario"),
                _ => RedirectToAction("Login")
            };
        }

        // Posteriormente: consultar o usuário pelo UID, comparar o hash da senha,
        // criar o cookie de autenticação (ASP.NET Core) e devolver o perfil.
        // Enquanto não for implementado, devolve null (login recusado).
        private string? ValidarCredenciais(string uid, string senha)
        {
            return null;
        }

        // =====================================================
        // ESQUECI MINHA SENHA
        // =====================================================

        [HttpGet]
        public IActionResult RecuperacaoSenha()
        {
            return View();
        }

        // Conforme a tela: "A solicitação será enviada ao administrador para análise e aprovação."
        [HttpPost]
        public IActionResult RecuperacaoSenha(string nomeUsuario, string uid, string email)
        {
            if (string.IsNullOrWhiteSpace(nomeUsuario) ||
                string.IsNullOrWhiteSpace(uid) ||
                string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Preencha todos os campos.");
                return View();
            }

            // Posteriormente: registrar a solicitação e gerar uma notificação para o administrador
            // (tela Notificações). O administrador conclui em Gerenciamento de Usuários > Alteração de Senha.

            TempData["Sucesso"] = "Solicitação enviada ao administrador.";
            return RedirectToAction("Login");
        }

        // =====================================================
        // SAIR (botão "Sair" das duas sidebars)
        // =====================================================

        [HttpGet]
        public IActionResult Sair()
        {
            // Posteriormente: HttpContext.SignOutAsync().
            return RedirectToAction("Login");
        }
    }
}
