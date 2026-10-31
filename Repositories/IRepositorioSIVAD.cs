using SIVAD.Models;
using SIVAD.Models.ViewModels;

namespace SIVAD.Repositories;

public interface IRepositorioSIVAD
{
    // READ: consultas usadas pelo dashboard, estoque, usuários e relatórios.
    Task<List<Produto>> ListarProdutosAsync(bool somenteComEstoque = false, bool incluirInativos = false);
    Task<List<Funcionario>> ListarFuncionariosAsync();
    Task<List<Administrador>> ListarAdministradoresAsync();
    Task<List<Fornecedor>> ListarFornecedoresAsync();
    Task<List<Produto>> ListarProdutosEstoqueBaixoAsync();
    Task<List<CompraEstoque>> ListarComprasAsync();
    Task<DashboardViewModel> ObterDashboardAsync();
    Task<List<MovimentoRelatorioViewModel>> ListarMovimentosRelatorioAsync();

    // PEDIDOS: CREATE do cabeçalho/itens e UPDATE do status com efeito no estoque.
    Task<int> RegistrarPedidoAsync(Pedido pedido);
    Task<bool> AtualizarStatusPedidoAsync(int codigo, int status);
    Task<Pedido?> ObterPedidoAsync(int codigo);
    Task<Pedido?> ObterPrimeiroPedidoAsync();
    Task<Pedido?> ObterUltimoPedidoPendenteAsync(int funcionarioId);

    // COMPRAS: CREATE, READ, UPDATE e cancelamento com atualização transacional do estoque.
    Task<int> RegistrarCompraEstoqueAsync(CompraEstoque compra);
    Task<CompraEstoque?> ObterCompraEstoqueAsync(int numero);
    Task<bool> AtualizarCompraEstoqueAsync(CompraEstoque compra);
    Task<bool> CancelarCompraEstoqueAsync(int numero);
    Task<CompraEstoque?> ObterPrimeiraCompraEstoqueAsync();

    // PRODUTOS: UPDATE do cadastro e devolução como reversão controlada de estoque.
    Task<bool> AtualizarProdutoAsync(Produto produto);
    Task<int> RegistrarDevolucaoAsync(int produtoCodigo, int quantidade);

    // FUNCIONÁRIOS: CREATE, UPDATE e DELETE lógico por desativação.
    Task<int> CadastrarFuncionarioAsync(string nome, string email, string senhaHash);
    Task<bool> DesativarFuncionarioAsync(int id);
    Task<bool> AtualizarFuncionarioAsync(int id, string nome, string? senhaHash, int? solicitacaoId);
    Task<CredencialAcesso?> ObterCredencialAcessoAsync(int pessoaId);
    Task<bool> ExisteAdministradorConfiguradoAsync();
    Task<bool> ConfigurarPrimeiroAdministradorAsync(int pessoaId, string cpf, string email, string senhaHash);
    Task<bool> ConfigurarAcessoFuncionarioAsync(int pessoaId, string email, string senhaHash);
    Task<bool> RegistrarSolicitacaoRecuperacaoSenhaAsync(int pessoaId, string nome, string email);
    Task<List<SolicitacaoRecuperacaoSenhaViewModel>> ListarSolicitacoesRecuperacaoSenhaAsync();
    Task<bool> RejeitarSolicitacaoRecuperacaoSenhaAsync(int solicitacaoId);
    Task<List<Categoria>> ListarCategoriasAsync();
    Task<int> CadastrarProdutoAsync(Produto produto);
    Task<InformacaoConexaoBancoViewModel> ObterDiagnosticoBancoAsync();
}
