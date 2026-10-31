using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;
using SIVAD.Repositories;
using SIVAD.Filtros;

namespace SIVAD.Controllers;

[ExigirSessao("Administrador")]
public class ProdutoController : Controller
{
    private readonly IRepositorioSIVAD _repository;
    public ProdutoController(IRepositorioSIVAD repository) => _repository = repository;

    [HttpGet]
    public IActionResult Index() => RedirectToAction("Estoque", "Administrador");

    [HttpPost]
    [ValidateAntiForgeryToken]
    // CREATE: cadastra o produto e retorna o ID gerado pelo banco.
    public async Task<IActionResult> Cadastrar(Produto produto)
    {
        if (!ModelState.IsValid)
        {
            TempData["Erro"] = "Confira os valores informados para o produto e tente novamente.";
            return RedirectToAction("Estoque", "Administrador");
        }
        try
        {
            var id = await _repository.CadastrarProdutoAsync(produto);
            TempData["Sucesso"] = $"Produto #{id} cadastrado com sucesso.";
        }
        catch (InvalidOperationException ex) { TempData["Erro"] = ex.Message; }
        return RedirectToAction("Estoque", "Administrador");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // UPDATE: altera os campos do produto ativo identificado pelo código.
    public async Task<IActionResult> Editar(Produto produto)
    {
        if (!ModelState.IsValid)
        {
            TempData["Erro"] = "Confira os valores informados para o produto e tente novamente.";
            return RedirectToAction("Estoque", "Administrador");
        }
        try
        {
            if (!await _repository.AtualizarProdutoAsync(produto)) TempData["Erro"] = "Produto não encontrado ou já removido.";
            else TempData["Sucesso"] = "Produto atualizado com sucesso.";
        }
        catch (InvalidOperationException ex) { TempData["Erro"] = ex.Message; }
        return RedirectToAction("Estoque", "Administrador");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // DELETE de negócio: registra devolução; não apaga o produto nem seu histórico.
    public async Task<IActionResult> RegistrarDevolucao(int produtoCodigo, int quantidade)
    {
        try
        {
            var numero = await _repository.RegistrarDevolucaoAsync(produtoCodigo, quantidade);
            TempData["Sucesso"] = $"Devolução #{numero} registrada: {quantidade} unidade(s) adicionada(s) ao estoque.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction("Estoque", "Administrador");
    }
}
