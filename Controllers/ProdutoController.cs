using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;

namespace SIVAD.Controllers
{
    // Controlador de Catálogo (Nível 3): cadastro, edição e exclusão de produtos.
    // Não possui views próprias: a tela é Views/Administrador/Estoque.cshtml
    // (botão "Adicionar Produto" e ícones de editar/excluir de cada linha).
    // Todas as ações terminam voltando para a tela Estoque.
    public class ProdutoController : Controller
    {
        // Não existe Views/Produto/Index.cshtml.
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Estoque", "Administrador");
        }

        // Botão "Adicionar Produto"
        [HttpPost]
        public IActionResult Cadastrar(Produto produto)
        {
            if (!ModelState.IsValid)
            {
                TempData["Erro"] = "Verifique os dados do produto.";
                return RedirectToAction("Estoque", "Administrador");
            }

            // Posteriormente: persistir o produto (tabela Produtos) com a categoria e o estoque inicial.

            TempData["Sucesso"] = "Produto cadastrado.";
            return RedirectToAction("Estoque", "Administrador");
        }

        // Ícone de editar
        [HttpPost]
        public IActionResult Editar(Produto produto)
        {
            if (!ModelState.IsValid)
            {
                TempData["Erro"] = "Verifique os dados do produto.";
                return RedirectToAction("Estoque", "Administrador");
            }

            // Posteriormente: atualizar o produto no banco.

            TempData["Sucesso"] = "Produto atualizado.";
            return RedirectToAction("Estoque", "Administrador");
        }

        // Ícone de lixeira
        [HttpPost]
        public IActionResult Excluir(int id)
        {
            // Posteriormente: excluir o produto (ou bloquear se houver itens de pedido/compra associados).

            TempData["Sucesso"] = "Produto removido.";
            return RedirectToAction("Estoque", "Administrador");
        }
    }
}
