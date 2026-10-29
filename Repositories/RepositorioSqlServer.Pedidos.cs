using System.Data;
using Microsoft.Data.SqlClient;
using SIVAD.Models;
using SIVAD.Models.ViewModels;

namespace SIVAD.Repositories;

/// <summary>Registro, consulta e atualização de pedidos e seus itens.</summary>
public sealed partial class RepositorioSqlServer
{
    // ================================================================
    // PEDIDOS — INSERÇÃO (INSERT) do cabeçalho e dos itens
    // ================================================================
    // O preço é relido do banco; nunca confiamos no valor enviado pelo navegador.
    // A transação grava Pedidos e Itens_Pedidos como uma única operação.
    public async Task<int> RegistrarPedidoAsync(Pedido pedido)
    {
        if (pedido.Itens.Count == 0 || pedido.Itens.Any(item => item.Qtd <= 0))
            throw new InvalidOperationException("Informe pelo menos um produto com quantidade maior que zero.");
        if (pedido.Itens.GroupBy(item => item.ProdutoCodigo).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Cada produto deve aparecer uma única vez no pedido.");
        if (pedido.ClienteId is <= 0 || pedido.FuncionarioId <= 0)
            throw new InvalidOperationException("O pedido precisa estar associado a um funcionário válido.");

        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync();
        // Primeiro validamos preço e estoque de todos os itens; só depois inserimos.
        decimal total = 0;
        foreach (var item in pedido.Itens)
        {
            await using var comandoPreco = new SqlCommand("SELECT preco_unit,estoque FROM Produtos WHERE codigo=@codigo AND ativo=1", conexao, transacao);
            AdicionarInteiro(comandoPreco, "@codigo", item.ProdutoCodigo);
            decimal precoUnitario;
            int estoqueDisponivel;
            await using (var leitor = await comandoPreco.ExecuteReaderAsync())
            {
                if (!await leitor.ReadAsync()) throw new InvalidOperationException($"O produto {item.ProdutoCodigo} não existe ou está inativo.");
                precoUnitario = leitor.GetDecimal(0);
                estoqueDisponivel = leitor.GetInt32(1);
            }
            if (item.Qtd > estoqueDisponivel) throw new InvalidOperationException($"Estoque insuficiente para o produto {item.ProdutoCodigo}; disponível: {estoqueDisponivel}.");
            var totalItem = precoUnitario * item.Qtd;
            item.PrecoTotal = totalItem;
            total += totalItem;
        }
        // Cabeçalho do pedido: OUTPUT devolve o código para relacionar os itens.
        const string sqlInserirPedido = """
            INSERT INTO Pedidos (status, valor_total, cliente_id, funcionario_id, data_pedido)
            OUTPUT INSERTED.codigo
            VALUES (@status, @total, @clienteCodigo, @funcionarioCodigo, GETDATE());
            """;
        await using (var comando = new SqlCommand(sqlInserirPedido, conexao, transacao))
        {
            AdicionarInteiro(comando, "@status", 0);
            AdicionarValorMonetario(comando, "@total", total);
            comando.Parameters.Add("@clienteCodigo", SqlDbType.Int).Value = pedido.ClienteId is null ? DBNull.Value : pedido.ClienteId.Value;
            AdicionarInteiro(comando, "@funcionarioCodigo", pedido.FuncionarioId);
            pedido.Codigo = Convert.ToInt32(await comando.ExecuteScalarAsync());
        }
        // Detalhes: cada item aponta para o pedido recém-criado.
        foreach (var item in pedido.Itens)
        {
            await using var comando = new SqlCommand("INSERT INTO Itens_Pedidos (pedido_codigo, produto_codigo, qtd, preco_total) VALUES (@numeroPedido,@produtoCodigo,@quantidade,@total)", conexao, transacao);
            AdicionarInteiro(comando, "@numeroPedido", pedido.Codigo); AdicionarInteiro(comando, "@produtoCodigo", item.ProdutoCodigo);
            AdicionarInteiro(comando, "@quantidade", item.Qtd); AdicionarValorMonetario(comando, "@total", item.PrecoTotal);
            await comando.ExecuteNonQueryAsync();
        }
        await transacao.CommitAsync();
        pedido.ValorTotal = total;
        return pedido.Codigo;
    }

    // ATUALIZAÇÃO de pedido: 0=pendente, 1=finalizado, 2=cancelado.
    // Finalizar baixa estoque; cancelar um pedido finalizado repõe as unidades.
    public async Task<bool> AtualizarStatusPedidoAsync(int codigo, int status)
    {
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync();
        int atual;
        // UPDLOCK protege a leitura do status até a gravação, evitando duas finalizações concorrentes.
        await using (var consultaStatus = new SqlCommand("SELECT status FROM Pedidos WITH (UPDLOCK) WHERE codigo=@codigo", conexao, transacao))
        {
            AdicionarInteiro(consultaStatus, "@codigo", codigo);
            var valor = await consultaStatus.ExecuteScalarAsync();
            if (valor is null) return false;
            atual = Convert.ToInt32(valor);
        }
        if (atual == status) return false;
        if (status is not (1 or 2)) throw new InvalidOperationException("Status de pedido inválido.");
        if (status == 1 && atual != 0) return false;
        if (status == 2 && atual is not (0 or 1)) return false;
        // Pendente -> finalizado: saída de mercadoria.
        if (status == 1 && atual == 0)
        {
            var itens = await LerItensPedidoNaTransacaoAsync(conexao, transacao, codigo);
            foreach (var item in itens)
            {
                await using var atualizacao = new SqlCommand("UPDATE Produtos SET estoque=estoque-@quantidade WHERE codigo=@produtoCodigo AND estoque>=@quantidade", conexao, transacao);
                AdicionarInteiro(atualizacao, "@quantidade", item.Qtd); AdicionarInteiro(atualizacao, "@produtoCodigo", item.ProdutoCodigo);
                if (await atualizacao.ExecuteNonQueryAsync() == 0) throw new InvalidOperationException($"Estoque insuficiente para o produto {item.ProdutoCodigo}.");
            }
        }
        // Finalizado -> cancelado: reposição das unidades vendidas.
        else if (status == 2 && atual == 1)
        {
            var itens = await LerItensPedidoNaTransacaoAsync(conexao, transacao, codigo);
            foreach (var item in itens)
            {
                await using var atualizacao = new SqlCommand("UPDATE Produtos SET estoque=estoque+@quantidade WHERE codigo=@produtoCodigo", conexao, transacao);
                AdicionarInteiro(atualizacao, "@quantidade", item.Qtd); AdicionarInteiro(atualizacao, "@produtoCodigo", item.ProdutoCodigo);
                await atualizacao.ExecuteNonQueryAsync();
            }
        }
        await using (var atualizacao = new SqlCommand("UPDATE Pedidos SET status=@status WHERE codigo=@codigo", conexao, transacao))
        {
            AdicionarInteiro(atualizacao, "@status", status); AdicionarInteiro(atualizacao, "@codigo", codigo);
            await atualizacao.ExecuteNonQueryAsync();
        }
        await transacao.CommitAsync();
        return true;
    }

    public async Task<Pedido?> ObterPedidoAsync(int codigo)
    {
        const string sql = "SELECT codigo,status,valor_total,cliente_id,funcionario_id FROM Pedidos WHERE codigo=@codigo";
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        Pedido? pedido;
        await using (var comando = new SqlCommand(sql, conexao))
        {
            AdicionarInteiro(comando, "@codigo", codigo);
            await using var leitor = await comando.ExecuteReaderAsync();
            if (!await leitor.ReadAsync()) return null;
            pedido = new Pedido
            {
                Codigo = leitor.GetInt32(0),
                Status = leitor.GetInt32(1),
                ValorTotal = leitor.GetDecimal(2),
                ClienteId = leitor.IsDBNull(3) ? null : leitor.GetInt32(3),
                FuncionarioId = leitor.GetInt32(4)
            };
        }
        pedido.Itens = await LerItensPedidoAsync(conexao, codigo);
        return pedido;
    }

    public async Task<Pedido?> ObterPrimeiroPedidoAsync()
    {
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        int? codigo;
        await using (var comando = new SqlCommand("SELECT TOP (1) codigo FROM Pedidos ORDER BY codigo", conexao))
        {
            var resultado = await comando.ExecuteScalarAsync();
            codigo = resultado is null ? null : Convert.ToInt32(resultado);
        }
        return codigo.HasValue ? await ObterPedidoAsync(codigo.Value) : null;
    }

    public async Task<Pedido?> ObterUltimoPedidoPendenteAsync()
    {
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        var resultado = await new SqlCommand("SELECT TOP (1) codigo FROM Pedidos WHERE status=0 ORDER BY codigo DESC", conexao).ExecuteScalarAsync();
        return resultado is null || resultado is DBNull ? null : await ObterPedidoAsync(Convert.ToInt32(resultado));
    }

    private static async Task<List<ItemPedido>> LerItensPedidoAsync(SqlConnection conexao, int codigo)
    {
        const string sql = """
            SELECT i.pedido_codigo,i.produto_codigo,i.qtd,i.preco_total,p.nome,p.codigo_barras,p.estoque,p.categoria_codigo,c.nome
            FROM Itens_Pedidos i INNER JOIN Produtos p ON p.codigo=i.produto_codigo
            INNER JOIN Categorias c ON c.codigo=p.categoria_codigo WHERE i.pedido_codigo=@codigo ORDER BY p.nome;
            """;
        var itens = new List<ItemPedido>();
        await using var comando = new SqlCommand(sql, conexao); AdicionarInteiro(comando, "@codigo", codigo);
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync()) itens.Add(new ItemPedido
        {
            PedidoCodigo = leitor.GetInt32(0),
            ProdutoCodigo = leitor.GetInt32(1),
            Qtd = leitor.GetInt32(2),
            PrecoTotal = leitor.GetDecimal(3),
            Produto = new Produto
            {
                Codigo = leitor.GetInt32(1),
                Nome = leitor.GetString(4),
                CodigoBarras = leitor.GetString(5),
                Estoque = leitor.GetInt32(6),
                CategoriaCodigo = leitor.GetInt32(7),
                Categoria = new Categoria { Codigo = leitor.GetInt32(7), Nome = leitor.GetString(8) }
            }
        });
        return itens;
    }

    private static async Task<List<ItemPedido>> LerItensPedidoNaTransacaoAsync(SqlConnection conexao, SqlTransaction transacao, int codigo)
    {
        var itens = new List<ItemPedido>();
        await using var comando = new SqlCommand("SELECT produto_codigo,qtd FROM Itens_Pedidos WHERE pedido_codigo=@codigo", conexao, transacao);
        AdicionarInteiro(comando, "@codigo", codigo);
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync()) itens.Add(new ItemPedido { ProdutoCodigo = leitor.GetInt32(0), Qtd = leitor.GetInt32(1) });
        return itens;
    }
}
