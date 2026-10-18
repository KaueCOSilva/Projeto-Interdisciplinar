using SIVAD.Models;
using SIVAD.Models.ViewModels;

namespace SIVAD.Repositories;

public interface ISIVADRepository
{
    Task<List<Produto>> ListarProdutosAsync(bool somenteComEstoque = false, bool incluirInativos = false);
    Task<List<Funcionario>> ListarFuncionariosAsync();
    Task<List<Administrador>> ListarAdministradoresAsync();
    Task<List<Fornecedor>> ListarFornecedoresAsync();
    Task<List<Produto>> ListarProdutosEstoqueBaixoAsync();
    Task<List<CompraEstoque>> ListarComprasAsync();
    Task<DashboardViewModel> ObterDashboardAsync();
    Task<List<ReportMovementViewModel>> ListarMovimentosRelatorioAsync();
    Task<int> RegistrarPedidoAsync(Pedido pedido);
    Task<bool> AtualizarStatusPedidoAsync(int codigo, int status);
    Task<Pedido?> ObterPedidoAsync(int codigo);
    Task<Pedido?> ObterPrimeiroPedidoAsync();
    Task<Pedido?> ObterUltimoPedidoPendenteAsync();
    Task<int> RegistrarCompraEstoqueAsync(CompraEstoque compra);
    Task<CompraEstoque?> ObterCompraEstoqueAsync(int numero);
    Task<bool> AtualizarCompraEstoqueAsync(CompraEstoque compra);
    Task<bool> CancelarCompraEstoqueAsync(int numero);
    Task<CompraEstoque?> ObterPrimeiraCompraEstoqueAsync();
    Task<bool> AtualizarProdutoAsync(Produto produto);
    Task<int> RegistrarDevolucaoAsync(int produtoCodigo, int quantidade);
    Task<int> CadastrarFuncionarioAsync(string nome);
    Task<bool> DesativarFuncionarioAsync(int id);
    Task<bool> AtualizarFuncionarioAsync(int id, string nome);
    Task<List<Categoria>> ListarCategoriasAsync();
    Task<int> CadastrarProdutoAsync(Produto produto);
    Task<DatabaseConnectionInfoViewModel> ObterDiagnosticoBancoAsync();
}
