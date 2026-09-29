using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;

namespace SIVAD.Controllers
{
    // Telas do administrador (documento 2.4.3):
    // Dashboard, Relatórios, Estoque, Movimento de Compras,
    // Funcionários > Cadastro de Funcionário / Gerenciamento de Usuários, Notificações.
 
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

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        // =====================================================
        // TELAS DE CONSULTA (menu lateral)
        // =====================================================

        // Abas da tela: diarios, mensal, anuais, devolucoes, fluxo-caixa
        [HttpGet]
        public IActionResult Relatorios(string periodo = "mensal")
        {
            ViewBag.Periodo = periodo;
            return View();
        }

        [HttpGet]
        public IActionResult Estoque()
        {
            return View();
        }

        [HttpGet]
        public IActionResult MovimentoCompras()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Notificacoes()
        {
            return View();
        }

        // =====================================================
        // FUNCIONÁRIOS > CADASTRO DE FUNCIONÁRIO
        // =====================================================

        [HttpGet]
        public IActionResult CadastrarUsuario()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CadastrarUsuario(Pessoa pessoa)
        {
            if (!ModelState.IsValid)
            {
                return View(pessoa); // permanece na tela de cadastro para corrigir os campos
            }

            // Posteriormente:
            // 1. Criar o funcionário via FuncionarioFactory.
            // 2. Gerar o UID e salvar a senha com hash.
            // 3. Salvar no banco.

            TempData["Sucesso"] = "Funcionário cadastrado com sucesso.";
            return RedirectToAction("GerenciarUsuarios");
        }

        // =====================================================
        // FUNCIONÁRIOS > GERENCIAMENTO DE USUÁRIOS
        // =====================================================

        [HttpGet]
        public IActionResult GerenciarUsuarios()
        {
            // Posteriormente: enviar a lista de usuários cadastrados para a view.
            return View();
        }

        // Formulário "Alteração de Senha" (também atende o caso de uso "Recuperar Acesso")
        [HttpPost]
        public IActionResult AlterarSenha(string uid, string nomeUsuario, string novaSenha, string confirmarSenha)
        {
            if (string.IsNullOrWhiteSpace(uid) ||
                string.IsNullOrWhiteSpace(nomeUsuario) ||
                string.IsNullOrWhiteSpace(novaSenha))
            {
                TempData["Erro"] = "Preencha todos os campos.";
                return RedirectToAction("GerenciarUsuarios");
            }

            if (novaSenha != confirmarSenha)
            {
                TempData["Erro"] = "A confirmação da senha não confere.";
                return RedirectToAction("GerenciarUsuarios");
            }

            // Posteriormente: localizar o usuário pelo UID + nome e salvar o novo hash.

            TempData["Sucesso"] = "Senha alterada com sucesso.";
            return RedirectToAction("GerenciarUsuarios");
        }

        // Botão "Remover" da lista de usuários cadastrados
        [HttpPost]
        public IActionResult ExcluirUsuario(int id)
        {
            // Posteriormente: excluir do banco (impedir que o administrador exclua a si mesmo).

            TempData["Sucesso"] = "Usuário removido.";
            return RedirectToAction("GerenciarUsuarios");
        }
    }
}
