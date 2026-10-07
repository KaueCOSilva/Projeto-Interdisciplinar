using System.Data;
using Microsoft.Data.SqlClient;
using SIVAD.Models;

namespace SIVAD.Repositories;

/// <summary>
/// Acesso ao SQL Server com ADO.NET e comandos SQL parametrizados.
/// </summary>
public sealed class SqlServerRepository
{
    private readonly string _connectionString;

    public SqlServerRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A connection string 'DefaultConnection' não foi configurada.");
    }

    public async Task<int> RegistrarPedidoAsync(Pedido pedido)
    {
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        using (var command = new SqlCommand(
            """
            INSERT INTO Pedidos (status, valor_total, cliente_id, funcionario_id)
            OUTPUT INSERTED.codigo
            VALUES (@status, @valor_total, @cliente_id, @funcionario_id);
            """, connection, transaction))
        {
            AdicionarInt(command, "@status", pedido.Status);
            AdicionarDecimal(command, "@valor_total", pedido.ValorTotal);
            AdicionarInt(command, "@cliente_id", pedido.ClienteId);
            AdicionarInt(command, "@funcionario_id", pedido.FuncionarioId);

            var resultado = await command.ExecuteScalarAsync();
            pedido.Codigo = Convert.ToInt32(resultado);
        }

        foreach (var item in pedido.Itens)
        {
            using var command = new SqlCommand(
                """
                INSERT INTO Itens_Pedidos (pedido_codigo, produto_codigo, qtd, preco_total)
                VALUES (@pedido_codigo, @produto_codigo, @qtd, @preco_total);
                """, connection, transaction);

            item.PedidoCodigo = pedido.Codigo;
            AdicionarInt(command, "@pedido_codigo", pedido.Codigo);
            AdicionarInt(command, "@produto_codigo", item.ProdutoCodigo);
            AdicionarInt(command, "@qtd", item.Qtd);
            AdicionarDecimal(command, "@preco_total", item.PrecoTotal);
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return pedido.Codigo;
    }

    public async Task<bool> AtualizarStatusPedidoAsync(int codigo, int status)
    {
        await using var connection = CriarConexao();
        await connection.OpenAsync();

        using var command = new SqlCommand(
            "UPDATE Pedidos SET status = @status WHERE codigo = @codigo;", connection);
        AdicionarInt(command, "@status", status);
        AdicionarInt(command, "@codigo", codigo);

        return await command.ExecuteNonQueryAsync() > 0;
    }

    public Task<Pedido?> ObterPedidoAsync(int codigo)
    {
        return ObterPedidoAsync(
            "SELECT codigo, status, valor_total, cliente_id, funcionario_id " +
            "FROM Pedidos WHERE codigo = @codigo;",
            codigo);
    }

    public Task<Pedido?> ObterPrimeiroPedidoAsync()
    {
        return ObterPedidoAsync(
            "SELECT TOP (1) codigo, status, valor_total, cliente_id, funcionario_id " +
            "FROM Pedidos ORDER BY codigo;",
            null);
    }

    private async Task<Pedido?> ObterPedidoAsync(string consulta, int? codigo)
    {
        await using var connection = CriarConexao();
        await connection.OpenAsync();

        Pedido? pedido = null;
        using (var command = new SqlCommand(consulta, connection))
        {
            if (codigo.HasValue)
            {
                AdicionarInt(command, "@codigo", codigo.Value);
            }

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                pedido = new Pedido
                {
                    Codigo = reader.GetInt32(reader.GetOrdinal("codigo")),
                    Status = reader.GetInt32(reader.GetOrdinal("status")),
                    ValorTotal = reader.GetDecimal(reader.GetOrdinal("valor_total")),
                    ClienteId = reader.GetInt32(reader.GetOrdinal("cliente_id")),
                    FuncionarioId = reader.GetInt32(reader.GetOrdinal("funcionario_id"))
                };
            }
        }

        if (pedido is null)
        {
            return null;
        }

        pedido.Itens = await ObterItensPedidoAsync(connection, pedido.Codigo);
        return pedido;
    }

    private static async Task<List<ItemPedido>> ObterItensPedidoAsync(
        SqlConnection connection,
        int pedidoCodigo)
    {
        const string consulta =
            "SELECT pedido_codigo, produto_codigo, qtd, preco_total " +
            "FROM Itens_Pedidos WHERE pedido_codigo = @pedido_codigo;";
        var itens = new List<ItemPedido>();

        using var command = new SqlCommand(consulta, connection);
        AdicionarInt(command, "@pedido_codigo", pedidoCodigo);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            itens.Add(new ItemPedido
            {
                PedidoCodigo = reader.GetInt32(reader.GetOrdinal("pedido_codigo")),
                ProdutoCodigo = reader.GetInt32(reader.GetOrdinal("produto_codigo")),
                Qtd = reader.GetInt32(reader.GetOrdinal("qtd")),
                PrecoTotal = reader.GetDecimal(reader.GetOrdinal("preco_total"))
            });
        }

        return itens;
    }

    public async Task<int> RegistrarCompraEstoqueAsync(CompraEstoque compra)
    {
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        var possuiDataInformada = compra.DataCompra != default;
        var consulta = possuiDataInformada
            ? """
              INSERT INTO CompraEstoque (data_compra, total, status, administrador_id, fornecedor_id)
              OUTPUT INSERTED.numero
              VALUES (@data_compra, @total, @status, @administrador_id, @fornecedor_id);
              """
            : """
              INSERT INTO CompraEstoque (total, status, administrador_id, fornecedor_id)
              OUTPUT INSERTED.numero
              VALUES (@total, @status, @administrador_id, @fornecedor_id);
              """;

        using (var command = new SqlCommand(consulta, connection, transaction))
        {
            if (possuiDataInformada)
            {
                command.Parameters.Add("@data_compra", SqlDbType.DateTime).Value = compra.DataCompra;
            }

            AdicionarDecimal(command, "@total", compra.Total);
            AdicionarInt(command, "@status", compra.Status);
            AdicionarInt(command, "@administrador_id", compra.AdministradorId);
            AdicionarInt(command, "@fornecedor_id", compra.FornecedorId);

            var resultado = await command.ExecuteScalarAsync();
            compra.Numero = Convert.ToInt32(resultado);
        }

        foreach (var item in compra.Itens)
        {
            using var command = new SqlCommand(
                """
                INSERT INTO Itens_CompraEstoque (compra_numero, produto_codigo, quantidade, valor)
                VALUES (@compra_numero, @produto_codigo, @quantidade, @valor);
                """, connection, transaction);

            item.CompraNumero = compra.Numero;
            AdicionarInt(command, "@compra_numero", compra.Numero);
            AdicionarInt(command, "@produto_codigo", item.ProdutoCodigo);
            AdicionarInt(command, "@quantidade", item.Quantidade);
            AdicionarDecimal(command, "@valor", item.Valor);
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return compra.Numero;
    }

    public async Task<CompraEstoque?> ObterPrimeiraCompraEstoqueAsync()
    {
        await using var connection = CriarConexao();
        await connection.OpenAsync();

        CompraEstoque? compra = null;
        const string consulta =
            "SELECT TOP (1) numero, data_compra, total, status, administrador_id, fornecedor_id " +
            "FROM CompraEstoque ORDER BY numero;";

        using (var command = new SqlCommand(consulta, connection))
        {
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                compra = new CompraEstoque
                {
                    Numero = reader.GetInt32(reader.GetOrdinal("numero")),
                    DataCompra = reader.GetDateTime(reader.GetOrdinal("data_compra")),
                    Total = reader.GetDecimal(reader.GetOrdinal("total")),
                    Status = reader.GetInt32(reader.GetOrdinal("status")),
                    AdministradorId = reader.GetInt32(reader.GetOrdinal("administrador_id")),
                    FornecedorId = reader.GetInt32(reader.GetOrdinal("fornecedor_id"))
                };
            }
        }

        if (compra is null)
        {
            return null;
        }

        compra.Itens = await ObterItensCompraAsync(connection, compra.Numero);
        return compra;
    }

    private static async Task<List<ItemCompraEstoque>> ObterItensCompraAsync(
        SqlConnection connection,
        int compraNumero)
    {
        const string consulta =
            "SELECT compra_numero, produto_codigo, quantidade, valor " +
            "FROM Itens_CompraEstoque WHERE compra_numero = @compra_numero;";
        var itens = new List<ItemCompraEstoque>();

        using var command = new SqlCommand(consulta, connection);
        AdicionarInt(command, "@compra_numero", compraNumero);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            itens.Add(new ItemCompraEstoque
            {
                CompraNumero = reader.GetInt32(reader.GetOrdinal("compra_numero")),
                ProdutoCodigo = reader.GetInt32(reader.GetOrdinal("produto_codigo")),
                Quantidade = reader.GetInt32(reader.GetOrdinal("quantidade")),
                Valor = reader.GetDecimal(reader.GetOrdinal("valor"))
            });
        }

        return itens;
    }

    private SqlConnection CriarConexao() => new(_connectionString);

    private static void AdicionarInt(SqlCommand command, string nome, int valor)
    {
        command.Parameters.Add(nome, SqlDbType.Int).Value = valor;
    }

    private static void AdicionarDecimal(SqlCommand command, string nome, decimal valor)
    {
        var parametro = command.Parameters.Add(nome, SqlDbType.Decimal);
        parametro.Precision = 10;
        parametro.Scale = 2;
        parametro.Value = valor;
    }
}
