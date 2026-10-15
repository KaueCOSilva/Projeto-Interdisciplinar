using SIVAD.Models;
using SIVAD.Models.ViewModels;

namespace SIVAD.Repositories;

public interface ISIVADRepository
{
    Task<List<Produto>> ListarProdutosAsync(bool somenteComEstoque = false);
    Task<List<Cliente>> ListarClientesAsync();
    Task<int?> BuscarClienteIdPorCpfAsync(string cpf);
    Task<List<Funcionario>> ListarFuncionariosAsync();
    Task<List<Administrador>> ListarAdministradoresAsync();
    Task<List<Fornecedor>> ListarFornecedoresAsync();
    Task<List<Produto>> ListarProdutosEstoqueBaixoAsync();
    Task<List<CompraEstoque>> ListarComprasRecentesAsync(int quantidade = 5);
    Task<DashboardViewModel> ObterDashboardAsync();
    Task<List<ReportMovementViewModel>> ListarMovimentosRelatorioAsync();
    Task<int> RegistrarPedidoAsync(Pedido pedido);
    Task<bool> AtualizarStatusPedidoAsync(int codigo, int status);
    Task<Pedido?> ObterPedidoAsync(int codigo);
    Task<Pedido?> ObterPrimeiroPedidoAsync();
    Task<int> RegistrarCompraEstoqueAsync(CompraEstoque compra);
    Task<CompraEstoque?> ObterPrimeiraCompraEstoqueAsync();
    Task<bool> AtualizarProdutoAsync(Produto produto);
    Task<bool> DesativarProdutoAsync(int codigo);
    Task<int> CadastrarFuncionarioAsync(string nome);
    Task<bool> DesativarFuncionarioAsync(int id);
    Task<bool> AtualizarFuncionarioAsync(int id, string nome);
    Task<List<Categoria>> ListarCategoriasAsync();
    Task<int> CadastrarProdutoAsync(Produto produto);
    Task<DatabaseConnectionInfoViewModel> ObterDiagnosticoBancoAsync();
}
