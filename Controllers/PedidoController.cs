using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;
using SIVAD.Repositories;

namespace SIVAD.Controllers;

public class PedidoController : Controller
{
    private const int StatusPendente = 0;
    private const int StatusFinalizado = 1;
    private const int StatusCancelado = 2;
    private readonly ISIVADRepository _repository;

    public PedidoController(ISIVADRepository repository) => _repository = repository;

    [HttpGet]
    public IActionResult Index() => RedirectToAction("RegistrarPedido", "Funcionario");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(Pedido pedido, string cpfCliente)
    {
        var cpf = new string((cpfCliente ?? string.Empty).Where(char.IsDigit).ToArray());
        if (cpf.Length != 11)
        {
            TempData["Erro"] = "Digite um CPF válido com 11 números.";
            return RedirectToAction("RegistrarPedido", "Funcionario");
        }
        var clienteId = await _repository.BuscarClienteIdPorCpfAsync(cpf);
        if (clienteId is null)
        {
            TempData["Erro"] = "Esse CPF não foi encontrado na tabela de clientes do banco.";
            return RedirectToAction("RegistrarPedido", "Funcionario");
        }
        pedido.ClienteId = clienteId.Value;
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
    public async Task<IActionResult> Finalizar(int id)
    {
        try
        {
            var atualizado = await _repository.AtualizarStatusPedidoAsync(id, StatusFinalizado);
            if (!atualizado) return NotFound();
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
    public async Task<IActionResult> Cancelar(int id)
    {
        var atualizado = await _repository.AtualizarStatusPedidoAsync(id, StatusCancelado);
        if (!atualizado) return NotFound();
        TempData["Sucesso"] = "Pedido cancelado; estoque restaurado quando necessário.";
        return RedirectToAction("Relatorios", "Administrador");
    }
}
