using System.Data;
using Microsoft.Data.SqlClient;
using SIVAD.Models;
using SIVAD.Models.ViewModels;

namespace SIVAD.Repositories;

/// <summary>Consultas de produtos, funcionários, administradores, fornecedores e compras.</summary>
public sealed partial class RepositorioSqlServer
{
    // ================================================================
    // LEITURA (SELECT) — catálogo, pessoas, alertas e histórico
    // ================================================================
    // Cada método abre sua própria conexão, usa parâmetros e transforma
    // as linhas do SQL em objetos que os controladores entregam às telas.
    public async Task<List<Produto>> ListarProdutosAsync(bool somenteComEstoque = false, bool incluirInativos = false)
    {
        const string sql = """
            SELECT p.codigo, p.codigo_barras, p.nome, p.preco_unit, p.estoque,
                   p.categoria_codigo, c.nome AS categoria_nome, p.cor,
                   COALESCE((
                       SELECT TOP (1) ce.fornecedor_nome
                       FROM Itens_CompraEstoque ic
                       INNER JOIN CompraEstoque ce ON ce.numero = ic.compra_numero AND ce.status = 1
                       WHERE ic.produto_codigo = p.codigo
                       ORDER BY ce.data_compra DESC, ce.numero DESC
                   ), '') AS fornecedor_nome,
                   p.ativo
            FROM Produtos p
            INNER JOIN Categorias c ON c.codigo = p.categoria_codigo
            -- Funcionário recebe somente produtos ativos com saldo positivo;
            -- o administrador pode consultar inativos quando precisa editar histórico.
            WHERE (@incluirInativos = 1 OR p.ativo = 1)
              AND (@somenteComEstoque = 0 OR p.estoque > 0)
            ORDER BY p.nome, p.codigo;
            """;
        var produtos = new List<Produto>();
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        comando.Parameters.Add("@somenteComEstoque", SqlDbType.Bit).Value = somenteComEstoque;
        comando.Parameters.Add("@incluirInativos", SqlDbType.Bit).Value = incluirInativos;
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync())
        {
            produtos.Add(new Produto
            {
                Codigo = leitor.GetInt32(0),
                CodigoBarras = leitor.GetString(1),
                Nome = leitor.GetString(2),
                PrecoUnit = leitor.GetDecimal(3),
                Estoque = leitor.GetInt32(4),
                CategoriaCodigo = leitor.GetInt32(5),
                Categoria = new Categoria { Codigo = leitor.GetInt32(5), Nome = leitor.GetString(6) },
                Cor = leitor.GetString(7),
                FornecedorNome = leitor.GetString(8),
                Ativo = leitor.GetBoolean(9)
            });
        }
        return produtos;
    }

    public async Task<List<Funcionario>> ListarFuncionariosAsync()
    {
        const string sql = """
            SELECT p.id, p.nome FROM Pessoas p
            INNER JOIN Funcionarios f ON f.pessoa_id = p.id WHERE f.ativo = 1 ORDER BY p.nome;
            """;
        var lista = new List<Funcionario>();
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync()) lista.Add(new Funcionario { Id = leitor.GetInt32(0), Nome = leitor.GetString(1) });
        return lista;
    }

    public async Task<List<Administrador>> ListarAdministradoresAsync()
    {
        const string sql = """
            SELECT p.id, p.nome FROM Pessoas p
            INNER JOIN Administradores a ON a.pessoa_id = p.id ORDER BY p.nome;
            """;
        var lista = new List<Administrador>();
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync()) lista.Add(new Administrador { Id = leitor.GetInt32(0), Nome = leitor.GetString(1) });
        return lista;
    }

    public async Task<List<Fornecedor>> ListarFornecedoresAsync()
    {
        const string sql = """
            SELECT p.id, p.nome, f.cnpj FROM Pessoas p
            INNER JOIN Fornecedores f ON f.pessoa_id = p.id ORDER BY p.nome;
            """;
        var lista = new List<Fornecedor>();
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync()) lista.Add(new Fornecedor { Id = leitor.GetInt32(0), Nome = leitor.GetString(1), CNPJ = leitor.GetString(2) });
        return lista;
    }

    public async Task<List<Produto>> ListarProdutosEstoqueBaixoAsync()
    {
        const string sql = """
            SELECT p.codigo, p.codigo_barras, p.nome, p.preco_unit, p.estoque,
                   p.categoria_codigo, c.nome
            FROM Produtos p INNER JOIN Categorias c ON c.codigo = p.categoria_codigo
            WHERE p.ativo = 1 AND p.estoque <= 10
            ORDER BY p.estoque, p.nome;
            """;
        var lista = new List<Produto>();
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync())
            lista.Add(new Produto
            {
                Codigo = leitor.GetInt32(0),
                CodigoBarras = leitor.GetString(1),
                Nome = leitor.GetString(2),
                PrecoUnit = leitor.GetDecimal(3),
                Estoque = leitor.GetInt32(4),
                CategoriaCodigo = leitor.GetInt32(5),
                Categoria = new Categoria { Codigo = leitor.GetInt32(5), Nome = leitor.GetString(6) }
            });
        return lista;
    }

    public async Task<List<CompraEstoque>> ListarComprasAsync()
    {
        const string sql = """
            SELECT c.numero, c.data_compra, c.total, c.status,
                   c.administrador_id, c.fornecedor_id, COALESCE(c.fornecedor_nome,p.nome) AS fornecedor_nome
            FROM CompraEstoque c
            LEFT JOIN Fornecedores f ON f.pessoa_id = c.fornecedor_id
            LEFT JOIN Pessoas p ON p.id = f.pessoa_id
            ORDER BY c.data_compra DESC, c.numero DESC;
            """;
        var lista = new List<CompraEstoque>();
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync())
        {
            lista.Add(new CompraEstoque
            {
                Numero = leitor.GetInt32(0),
                DataCompra = leitor.GetDateTime(1),
                Total = leitor.GetDecimal(2),
                Status = leitor.GetInt32(3),
                AdministradorId = leitor.GetInt32(4),
                FornecedorId = leitor.IsDBNull(5) ? null : leitor.GetInt32(5),
                FornecedorNome = leitor.GetString(6),
                Fornecedor = new Fornecedor { Nome = leitor.GetString(6) }
            });
        }
        return lista;
    }
}
