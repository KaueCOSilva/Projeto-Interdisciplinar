using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;
using SIVAD.Repositories;

namespace SIVAD.Controllers;

public class ProdutoController : Controller
{
    private readonly ISIVADRepository _repository;
    public ProdutoController(ISIVADRepository repository) => _repository = repository;

    [HttpGet]
    public IActionResult Index() => RedirectToAction("Estoque", "Administrador");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cadastrar(Produto produto)
    {
        try
        {
            var id = await _repository.CadastrarProdutoAsync(produto);
            TempData["Sucesso"] = $"Produto #{id} cadastrado no banco de dados.";
        }
        catch (InvalidOperationException ex) { TempData["Erro"] = ex.Message; }
        return RedirectToAction("Estoque", "Administrador");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Produto produto)
    {
        try
        {
            if (!await _repository.AtualizarProdutoAsync(produto)) TempData["Erro"] = "Produto não encontrado ou já removido.";
            else TempData["Sucesso"] = "Produto atualizado no banco de dados.";
        }
        catch (InvalidOperationException ex) { TempData["Erro"] = ex.Message; }
        return RedirectToAction("Estoque", "Administrador");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int id)
    {
        if (id <= 0) return BadRequest();
        if (!await _repository.DesativarProdutoAsync(id)) TempData["Erro"] = "Produto não encontrado ou já removido.";
        else TempData["Sucesso"] = "Produto removido da lista. O histórico de pedidos foi preservado.";
        return RedirectToAction("Estoque", "Administrador");
    }
}
