using System.Data;
using Microsoft.Data.SqlClient;
using SIVAD.Models;
using SIVAD.Models.ViewModels;

namespace SIVAD.Repositories;

/// <summary>Registro, consulta, edição e cancelamento de compras de estoque.</summary>
public sealed partial class RepositorioSqlServer
{
    // ================================================================
    // COMPRAS DE ESTOQUE — INSERÇÃO (INSERT) e entrada de mercadoria
    // ================================================================
    // A compra gera cabeçalho, itens e aumento de estoque na mesma transação.
    public async Task<int> RegistrarCompraEstoqueAsync(CompraEstoque compra)
    {
        if (string.IsNullOrWhiteSpace(compra.FornecedorNome) || compra.FornecedorNome.Trim().Length > 150)
            throw new InvalidOperationException("Informe o nome do fornecedor (até 150 caracteres).");
        if (compra.AdministradorId <= 0 || compra.Itens.Count == 0 || compra.Itens.Any(item => item.Quantidade <= 0 || item.PrecoUnitario < 0))
            throw new InvalidOperationException("Informe um administrador, ao menos um item, quantidade positiva e preço válido.");
        if (compra.Itens.GroupBy(item => item.ProdutoCodigo).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Cada produto deve aparecer uma única vez na compra.");
        foreach (var item in compra.Itens)
            item.Valor = decimal.Round(item.PrecoUnitario * item.Quantidade, 2, MidpointRounding.AwayFromZero);
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync();
        // O total é calculado no servidor a partir de preço unitário x quantidade.
        var total = compra.Itens.Sum(item => item.Valor);
        const string sqlInserirPedido = """
            INSERT INTO CompraEstoque(data_compra,total,status,administrador_id,fornecedor_id,fornecedor_nome)
            OUTPUT INSERTED.numero VALUES(COALESCE(@data,GETDATE()),@total,1,@administradorCodigo,NULL,@nomeFornecedor);
            """;
        await using (var comando = new SqlCommand(sqlInserirPedido, conexao, transacao))
        {
            comando.Parameters.Add("@data", SqlDbType.DateTime).Value = compra.DataCompra == default ? DBNull.Value : compra.DataCompra;
            AdicionarValorMonetario(comando, "@total", total); AdicionarInteiro(comando, "@administradorCodigo", compra.AdministradorId);
            comando.Parameters.Add("@nomeFornecedor", SqlDbType.VarChar, 150).Value = compra.FornecedorNome.Trim();
            compra.Numero = Convert.ToInt32(await comando.ExecuteScalarAsync());
        }
        foreach (var item in compra.Itens)
        {
            var totalLinha = item.Valor;
            var corInformada = string.IsNullOrWhiteSpace(item.Cor) ? string.Empty : item.Cor.Trim();
            if (corInformada.Length > 60) throw new InvalidOperationException("A cor do produto deve ter até 60 caracteres.");
            await using (var atualizacao = new SqlCommand("UPDATE Produtos SET estoque=estoque+@quantidade,cor=CASE WHEN @cor='' THEN cor ELSE @cor END OUTPUT INSERTED.cor WHERE codigo=@produtoCodigo AND ativo=1 AND estoque<=2147483647-@quantidade", conexao, transacao))
            {
                AdicionarInteiro(atualizacao, "@quantidade", item.Quantidade); AdicionarInteiro(atualizacao, "@produtoCodigo", item.ProdutoCodigo);
                atualizacao.Parameters.Add("@cor", SqlDbType.VarChar, 60).Value = corInformada;
                var corSalva = await atualizacao.ExecuteScalarAsync();
                if (corSalva is null || corSalva is DBNull) throw new InvalidOperationException($"Não foi possível atualizar o produto {item.ProdutoCodigo}: confira se está ativo e se a quantidade não excede o limite de estoque.");
                item.Cor = Convert.ToString(corSalva) ?? "Não informada";
            }
            await using var comandoInserirItem = new SqlCommand("INSERT INTO Itens_CompraEstoque(compra_numero,produto_codigo,cor,quantidade,valor) VALUES(@numeroCompra,@produtoCodigo,@cor,@quantidade,@total)", conexao, transacao);
            AdicionarInteiro(comandoInserirItem, "@numeroCompra", compra.Numero); AdicionarInteiro(comandoInserirItem, "@produtoCodigo", item.ProdutoCodigo);
            comandoInserirItem.Parameters.Add("@cor", SqlDbType.VarChar, 60).Value = item.Cor;
            AdicionarInteiro(comandoInserirItem, "@quantidade", item.Quantidade); AdicionarValorMonetario(comandoInserirItem, "@total", totalLinha);
            await comandoInserirItem.ExecuteNonQueryAsync();
        }
        await transacao.CommitAsync();
        compra.Total = total;
        compra.Status = 1;
        return compra.Numero;
    }

    // LEITURA de uma compra: lê o cabeçalho e depois seus itens relacionados.
    public async Task<CompraEstoque?> ObterCompraEstoqueAsync(int numero)
    {
        const string sql = "SELECT numero,data_compra,total,status,administrador_id,fornecedor_id,fornecedor_nome FROM CompraEstoque WHERE numero=@numero";
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        CompraEstoque? compra;
        await using (var comando = new SqlCommand(sql, conexao))
        {
            AdicionarInteiro(comando, "@numero", numero);
            await using var leitor = await comando.ExecuteReaderAsync();
            if (!await leitor.ReadAsync()) return null;
            compra = new CompraEstoque
            {
                Numero = leitor.GetInt32(0),
                DataCompra = leitor.GetDateTime(1),
                Total = leitor.GetDecimal(2),
                Status = leitor.GetInt32(3),
                AdministradorId = leitor.GetInt32(4),
                FornecedorId = leitor.IsDBNull(5) ? null : leitor.GetInt32(5),
                FornecedorNome = leitor.GetString(6)
            };
        }
        compra.Itens = await LerItensCompraAsync(conexao, numero);
        return compra;
    }

    // ATUALIZAÇÃO de compra: desfaz a quantidade antiga, aplica a nova e recria os itens.
    // A diferença é calculada por produto para não duplicar entradas no estoque.
    public async Task<bool> AtualizarCompraEstoqueAsync(CompraEstoque compra)
    {
        if (compra.Numero <= 0 || string.IsNullOrWhiteSpace(compra.FornecedorNome) || compra.FornecedorNome.Trim().Length > 150)
            throw new InvalidOperationException("Informe um nome de fornecedor válido (até 150 caracteres).");
        if (compra.Itens.Count == 0 || compra.Itens.Any(item => item.ProdutoCodigo <= 0 || item.Quantidade <= 0 || item.PrecoUnitario < 0))
            throw new InvalidOperationException("A compra precisa ter ao menos um item com produto, quantidade positiva e preço válido.");
        if (compra.Itens.GroupBy(item => item.ProdutoCodigo).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Cada produto deve aparecer uma única vez na compra.");

        foreach (var item in compra.Itens)
        {
            item.Cor = string.IsNullOrWhiteSpace(item.Cor) ? "Não informada" : item.Cor.Trim();
            if (item.Cor.Length > 60)
                throw new InvalidOperationException("A cor do produto deve ter até 60 caracteres.");
            item.Valor = decimal.Round(item.PrecoUnitario * item.Quantidade, 2, MidpointRounding.AwayFromZero);
        }
        var novoTotal = compra.Itens.Sum(item => item.Valor);

        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync(IsolationLevel.Serializable);
        int statusAtual;
        DateTime dataAtual;
        await using (var cabecalho = new SqlCommand("SELECT status,data_compra FROM CompraEstoque WITH (UPDLOCK,HOLDLOCK) WHERE numero=@numero", conexao, transacao))
        {
            AdicionarInteiro(cabecalho, "@numero", compra.Numero);
            await using var leitor = await cabecalho.ExecuteReaderAsync();
            if (!await leitor.ReadAsync())
            {
                await leitor.CloseAsync();
                await transacao.RollbackAsync();
                return false;
            }
            statusAtual = leitor.GetInt32(0);
            dataAtual = leitor.GetDateTime(1);
        }
        if (statusAtual != 1)
        {
            await transacao.RollbackAsync();
            return false;
        }
        if (compra.DataCompra == default) compra.DataCompra = dataAtual;

        // Snapshot dos itens antes da edição: será comparado com o novo formulário.
        var quantidadesAnteriores = new Dictionary<int, int>();
        await using (var consultaItensAntigos = new SqlCommand("SELECT produto_codigo,quantidade FROM Itens_CompraEstoque WITH (UPDLOCK,HOLDLOCK) WHERE compra_numero=@numero", conexao, transacao))
        {
            AdicionarInteiro(consultaItensAntigos, "@numero", compra.Numero);
            await using var leitor = await consultaItensAntigos.ExecuteReaderAsync();
            while (await leitor.ReadAsync()) quantidadesAnteriores[leitor.GetInt32(0)] = leitor.GetInt32(1);
        }
        var quantidadesNovas = compra.Itens.ToDictionary(item => item.ProdutoCodigo, item => item.Quantidade);
        // A diferença positiva aumenta estoque; a negativa retira apenas o excedente.
        foreach (var produtoCodigo in quantidadesAnteriores.Keys.Union(quantidadesNovas.Keys).OrderBy(id => id))
        {
            quantidadesAnteriores.TryGetValue(produtoCodigo, out var quantidadeAnterior);
            quantidadesNovas.TryGetValue(produtoCodigo, out var quantidadeNova);
            var diferenca = quantidadeNova - quantidadeAnterior;
            if (diferenca == 0) continue;

            var adicionar = diferenca > 0;
            var quantidade = Math.Abs(diferenca);
            var sqlEstoque = adicionar
                ? "UPDATE Produtos SET estoque=estoque+@quantidade WHERE codigo=@produtoCodigo AND ativo=1 AND estoque<=2147483647-@quantidade"
                : "UPDATE Produtos SET estoque=estoque-@quantidade WHERE codigo=@produtoCodigo AND estoque>=@quantidade";
            await using var atualizarEstoque = new SqlCommand(sqlEstoque, conexao, transacao);
            AdicionarInteiro(atualizarEstoque, "@quantidade", quantidade);
            AdicionarInteiro(atualizarEstoque, "@produtoCodigo", produtoCodigo);
            if (await atualizarEstoque.ExecuteNonQueryAsync() != 1)
            {
                var motivo = adicionar
                    ? "não existe ou está inativo"
                    : "não possui saldo suficiente para retirar a diferença (parte do estoque pode já ter sido vendida)";
                throw new InvalidOperationException($"Não foi possível editar a compra #{compra.Numero}: o produto {produtoCodigo} {motivo}.");
            }
        }

        await using (var atualizarCabecalho = new SqlCommand("UPDATE CompraEstoque SET data_compra=@data,total=@total,fornecedor_id=NULL,fornecedor_nome=@nomeFornecedor WHERE numero=@numero AND status=1", conexao, transacao))
        {
            atualizarCabecalho.Parameters.Add("@data", SqlDbType.DateTime).Value = compra.DataCompra;
            AdicionarValorMonetario(atualizarCabecalho, "@total", novoTotal);
            atualizarCabecalho.Parameters.Add("@nomeFornecedor", SqlDbType.VarChar, 150).Value = compra.FornecedorNome.Trim();
            AdicionarInteiro(atualizarCabecalho, "@numero", compra.Numero);
            if (await atualizarCabecalho.ExecuteNonQueryAsync() != 1)
                throw new InvalidOperationException("A compra deixou de estar disponível para edição.");
        }
        await using (var excluirItens = new SqlCommand("DELETE FROM Itens_CompraEstoque WHERE compra_numero=@numero", conexao, transacao))
        {
            AdicionarInteiro(excluirItens, "@numero", compra.Numero);
            await excluirItens.ExecuteNonQueryAsync();
        }
        foreach (var item in compra.Itens)
        {
            await using var inserirItem = new SqlCommand("INSERT INTO Itens_CompraEstoque(compra_numero,produto_codigo,cor,quantidade,valor) VALUES(@numeroCompra,@produtoCodigo,@cor,@quantidade,@valor)", conexao, transacao);
            AdicionarInteiro(inserirItem, "@numeroCompra", compra.Numero);
            AdicionarInteiro(inserirItem, "@produtoCodigo", item.ProdutoCodigo);
            inserirItem.Parameters.Add("@cor", SqlDbType.VarChar, 60).Value = item.Cor;
            AdicionarInteiro(inserirItem, "@quantidade", item.Quantidade);
            AdicionarValorMonetario(inserirItem, "@valor", item.Valor);
            await inserirItem.ExecuteNonQueryAsync();
        }

        await transacao.CommitAsync();
        compra.Total = novoTotal;
        compra.Status = 1;
        return true;
    }

    // CANCELAMENTO de negócio: cancela a compra, reverte o estoque e preserva o histórico.
    public async Task<bool> CancelarCompraEstoqueAsync(int numero)
    {
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync(IsolationLevel.Serializable);
        int status;
        await using (var cabecalho = new SqlCommand("SELECT status FROM CompraEstoque WITH (UPDLOCK,HOLDLOCK) WHERE numero=@numero", conexao, transacao))
        {
            AdicionarInteiro(cabecalho, "@numero", numero);
            var resultado = await cabecalho.ExecuteScalarAsync();
            if (resultado is null || resultado is DBNull)
            {
                await transacao.RollbackAsync();
                return false;
            }
            status = Convert.ToInt32(resultado);
        }
        if (status != 1)
        {
            await transacao.RollbackAsync();
            return false;
        }

        var itens = new Dictionary<int, int>();
        await using (var consultarItens = new SqlCommand("SELECT produto_codigo,quantidade FROM Itens_CompraEstoque WITH (UPDLOCK,HOLDLOCK) WHERE compra_numero=@numero", conexao, transacao))
        {
            AdicionarInteiro(consultarItens, "@numero", numero);
            await using var leitor = await consultarItens.ExecuteReaderAsync();
            while (await leitor.ReadAsync()) itens[leitor.GetInt32(0)] = leitor.GetInt32(1);
        }
        foreach (var item in itens.OrderBy(item => item.Key))
        {
            await using var atualizarEstoque = new SqlCommand("UPDATE Produtos SET estoque=estoque-@quantidade WHERE codigo=@produtoCodigo AND estoque>=@quantidade", conexao, transacao);
            AdicionarInteiro(atualizarEstoque, "@quantidade", item.Value);
            AdicionarInteiro(atualizarEstoque, "@produtoCodigo", item.Key);
            if (await atualizarEstoque.ExecuteNonQueryAsync() != 1)
                throw new InvalidOperationException($"Não é possível cancelar a compra #{numero}: o produto {item.Key} não tem saldo suficiente para retirar as unidades compradas. Ajuste o estoque antes de cancelar.");
        }

        await using (var atualizarCabecalho = new SqlCommand("UPDATE CompraEstoque SET status=2 WHERE numero=@numero AND status=1", conexao, transacao))
        {
            AdicionarInteiro(atualizarCabecalho, "@numero", numero);
            if (await atualizarCabecalho.ExecuteNonQueryAsync() != 1)
                throw new InvalidOperationException("A compra deixou de estar disponível para cancelamento.");
        }
        await transacao.CommitAsync();
        return true;
    }

    public async Task<CompraEstoque?> ObterPrimeiraCompraEstoqueAsync()
    {
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        int? numero;
        await using (var comando = new SqlCommand("SELECT TOP (1) numero FROM CompraEstoque ORDER BY numero", conexao))
        {
            var resultado = await comando.ExecuteScalarAsync();
            numero = resultado is null ? null : Convert.ToInt32(resultado);
        }
        if (!numero.HasValue) return null;
        await using var cabecalho = new SqlCommand("SELECT numero,data_compra,total,status,administrador_id,fornecedor_id,fornecedor_nome FROM CompraEstoque WHERE numero=@numero", conexao);
        AdicionarInteiro(cabecalho, "@numero", numero.Value);
        CompraEstoque compra;
        await using (var leitor = await cabecalho.ExecuteReaderAsync())
        {
            if (!await leitor.ReadAsync()) return null;
            compra = new CompraEstoque
            {
                Numero = leitor.GetInt32(0),
                DataCompra = leitor.GetDateTime(1),
                Total = leitor.GetDecimal(2),
                Status = leitor.GetInt32(3),
                AdministradorId = leitor.GetInt32(4),
                FornecedorId = leitor.IsDBNull(5) ? null : leitor.GetInt32(5),
                FornecedorNome = leitor.GetString(6)
            };
        }
        compra.Itens = await LerItensCompraAsync(conexao, compra.Numero);
        return compra;
    }
    private static async Task<List<ItemCompraEstoque>> LerItensCompraAsync(SqlConnection conexao, int numero)
    {
        var itens = new List<ItemCompraEstoque>();
        await using var comando = new SqlCommand("SELECT compra_numero,produto_codigo,quantidade,valor,cor FROM Itens_CompraEstoque WHERE compra_numero=@numero", conexao);
        AdicionarInteiro(comando, "@numero", numero);
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync()) itens.Add(new ItemCompraEstoque
        {
            CompraNumero = leitor.GetInt32(0),
            ProdutoCodigo = leitor.GetInt32(1),
            Quantidade = leitor.GetInt32(2),
            Valor = leitor.GetDecimal(3),
            Cor = leitor.GetString(4),
            PrecoUnitario = decimal.Round(leitor.GetDecimal(3) / leitor.GetInt32(2), 12, MidpointRounding.AwayFromZero)
        });
        return itens;
    }
}
