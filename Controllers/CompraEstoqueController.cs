using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SIVAD.Data;
using SIVAD.Models;
using SIVAD.Strategies;

namespace SIVAD.Controllers
{
    // Controlador de Gestão de Estoque (Nível 3): registra a compra junto ao fornecedor.
    // Não possui views próprias: a tela é Views/Administrador/MovimentoCompras.cshtml
    // e o formulário "Nova Compra" deve enviar para CompraEstoque/Registrar.
    public class CompraEstoqueController : Controller
    {
        
        private const int StatusPendente = 0;

        private readonly AppDbContext _context;
        private readonly TotalCompraEstoqueStrategy _totalCompraEstoqueStrategy;

        public CompraEstoqueController(AppDbContext context, TotalCompraEstoqueStrategy totalCompraEstoqueStrategy)
        {
            _context = context;
            _totalCompraEstoqueStrategy = totalCompraEstoqueStrategy;
        }

        // Não existe Views/CompraEstoque/Index.cshtml.
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("MovimentoCompras", "Administrador");
        }

        // Botão "Finalizar Compra" da tela Movimento de Compras.
        [HttpPost]
        public async Task<IActionResult> Registrar(CompraEstoque compraEstoque)
        {
            if (!ModelState.IsValid)
            {
                TempData["Erro"] = "Verifique os dados da compra.";
                return RedirectToAction("MovimentoCompras", "Administrador");
            }

            // Posteriormente:
            // - Associar o administrador logado e o fornecedor.
            // - Atualizar o estoque com os itens comprados.

            compraEstoque.Total = _totalCompraEstoqueStrategy.CalcularTotal(compraEstoque);
            compraEstoque.Status = StatusPendente;

            _context.CompraEstoque.Add(compraEstoque);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Compra registrada.";
            return RedirectToAction("MovimentoCompras", "Administrador");
        }
    }
}
