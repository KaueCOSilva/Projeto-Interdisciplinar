using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;
using SIVAD.Repositories;

namespace SIVAD.Controllers;

public class PedidoController : Controller
{
    private const int StatusPendente = 0;
    private const int StatusFinalizado = 1;
    private const int StatusCancelado = 2;
    private readonly IRepositorioSIVAD _repository;

    public PedidoController(IRepositorioSIVAD repository) => _repository = repository;

    [HttpGet]
    public IActionResult Index() => RedirectToAction("RegistrarPedido", "Funcionario");

    [HttpPost]
    [ValidateAntiForgeryToken]
    // CREATE: valida itens, calcula no servidor e grava pedido + itens.
    public async Task<IActionResult> Registrar(Pedido pedido)
    {
        pedido.ClienteId = null;
        pedido.Itens = pedido.Itens.Where(item => item.Qtd > 0).ToList();
        if (pedido.FuncionarioId <= 0 || pedido.Itens.Count == 0)
        {
            TempData["Erro"] = "Selecione o funcionário e informe pelo menos um produto com quantidade positiva.";
            return RedirectToAction("RegistrarPedido", "Funcionario");
        }
        try
        {
            pedido.Status = StatusPendente;
            // O Repository lê os preços vigentes no SQL Server e calcula o valor sem confiar no navegador.
            await _repository.RegistrarPedidoAsync(pedido);
            return RedirectToAction("ResumoCompra", "Funcionario", new { id = pedido.Codigo });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Erro"] = ex.Message;
            return RedirectToAction("RegistrarPedido", "Funcionario");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // UPDATE: finaliza o pedido e baixa o estoque na mesma transação.
    public async Task<IActionResult> Finalizar(int id)
    {
        if (id <= 0) return BadRequest();
        try
        {
            var atualizado = await _repository.AtualizarStatusPedidoAsync(id, StatusFinalizado);
            if (!atualizado)
            {
                var pedidoExistente = await _repository.ObterPedidoAsync(id);
                if (pedidoExistente is null) return NotFound();
                if (pedidoExistente.Status == StatusFinalizado)
                {
                    TempData["Sucesso"] = "Este pedido já havia sido contabilizado. O resumo foi encerrado e o histórico foi preservado.";
                    return RedirectToAction("RegistrarPedido", "Funcionario");
                }
                return NotFound();
            }
            TempData["Sucesso"] = "Pedido contabilizado e estoque atualizado.";
            return RedirectToAction("RegistrarPedido", "Funcionario");
        }
        catch (InvalidOperationException ex)
        {
            TempData["Erro"] = ex.Message;
            return RedirectToAction("ResumoCompra", "Funcionario", new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // DELETE de negócio: cancela o pedido e preserva seu histórico.
    public async Task<IActionResult> Cancelar(int id)
    {
        var atualizado = await _repository.AtualizarStatusPedidoAsync(id, StatusCancelado);
        if (!atualizado) return NotFound();
        TempData["Sucesso"] = "Pedido cancelado; estoque restaurado quando necessário.";
        return RedirectToAction("Relatorios", "Administrador");
    }
}
