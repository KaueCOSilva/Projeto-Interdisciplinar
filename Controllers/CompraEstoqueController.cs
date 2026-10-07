using Microsoft.AspNetCore.Mvc;
using SIVAD.Repositories;
using SIVAD.Models;
using SIVAD.Strategies;

namespace SIVAD.Controllers
{
    // Controlador de Gestão de Estoque (Nível 3): registra compras junto ao fornecedor.
    // Não possui views próprias: a tela é Views/Administrador/MovimentoCompras.cshtml.
    public class CompraEstoqueController : Controller
    {
        private const int StatusPendente = 0;

        private readonly SqlServerRepository _repository;
        private readonly TotalCompraEstoqueStrategy _totalCompraEstoqueStrategy;

        public CompraEstoqueController(
            SqlServerRepository repository,
            TotalCompraEstoqueStrategy totalCompraEstoqueStrategy)
        {
            _repository = repository;
            _totalCompraEstoqueStrategy = totalCompraEstoqueStrategy;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("MovimentoCompras", "Administrador");
        }

        [HttpPost]
        public async Task<IActionResult> Registrar(CompraEstoque compraEstoque)
        {
            if (!ModelState.IsValid)
            {
                TempData["Erro"] = "Verifique os dados da compra.";
                return RedirectToAction("MovimentoCompras", "Administrador");
            }

            // A associação do administrador e do fornecedor ainda precisa ser ligada à interface.
            compraEstoque.Total = _totalCompraEstoqueStrategy.CalcularTotal(compraEstoque);
            compraEstoque.Status = StatusPendente;

            await _repository.RegistrarCompraEstoqueAsync(compraEstoque);

            TempData["Sucesso"] = "Compra registrada.";
            return RedirectToAction("MovimentoCompras", "Administrador");
        }
    }
}
