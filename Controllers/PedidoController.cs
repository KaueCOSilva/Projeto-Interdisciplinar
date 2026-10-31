using Microsoft.AspNetCore.Mvc;
using SIVAD.Models;
using SIVAD.Repositories;
using SIVAD.Filtros;

namespace SIVAD.Controllers;

[ExigirSessao]
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
    // CREATE: valida itens, usa o funcionário da sessão e grava pedido + itens.
    public async Task<IActionResult> Registrar(Pedido pedido)
    {
        if (!ModelState.IsValid)
        {
            TempData["Erro"] = "Revise os dados e as quantidades do pedido.";
            return RedirectToAction("RegistrarPedido", "Funcionario");
        }

        pedido.ClienteId = null;
        pedido.Itens = (pedido.Itens ?? new List<ItemPedido>())
            .Where(item => item.Qtd > 0)
            .ToList();

        if (HttpContext.Session.GetString("perfil") == "Funcionario")
        {
            var funcionarioSessao = HttpContext.Session.GetInt32("id");
            if (funcionarioSessao is null) return Unauthorized();
            // Não confiamos no FuncionarioId recebido do formulário.
            pedido.FuncionarioId = funcionarioSessao.Value;
        }

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
    // UPDATE: somente o funcionário proprietário ou um administrador pode finalizar.
    public async Task<IActionResult> Finalizar(int id)
    {
        if (id <= 0) return BadRequest();
        try
        {
            var pedidoExistente = await _repository.ObterPedidoAsync(id);
            if (pedidoExistente is null) return NotFound();
            if (!PodeGerenciarPedido(pedidoExistente)) return Forbid();

            var atualizado = await _repository.AtualizarStatusPedidoAsync(id, StatusFinalizado);
            if (!atualizado)
            {
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
        if (id <= 0) return BadRequest();
        try
        {
            var pedidoExistente = await _repository.ObterPedidoAsync(id);
            if (pedidoExistente is null) return NotFound();
            if (!PodeGerenciarPedido(pedidoExistente)) return Forbid();

            var atualizado = await _repository.AtualizarStatusPedidoAsync(id, StatusCancelado);
            if (!atualizado) return NotFound();
            TempData["Sucesso"] = "Pedido cancelado; estoque restaurado quando necessário.";
            return RedirecionarAposCancelamento();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Erro"] = ex.Message;
            return RedirecionarAposCancelamento();
        }
    }

    private bool PodeGerenciarPedido(Pedido pedido)
    {
        var perfil = HttpContext.Session.GetString("perfil");
        return perfil == "Administrador"
            || (perfil == "Funcionario" && HttpContext.Session.GetInt32("id") == pedido.FuncionarioId);
    }

    private IActionResult RedirecionarAposCancelamento() =>
        HttpContext.Session.GetString("perfil") == "Administrador"
            ? RedirectToAction("Relatorios", "Administrador")
            : RedirectToAction("RegistrarPedido", "Funcionario");
}
