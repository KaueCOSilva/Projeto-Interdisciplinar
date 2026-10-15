using System.Data;
using Microsoft.Data.SqlClient;
using SIVAD.Models;
using SIVAD.Models.ViewModels;

namespace SIVAD.Repositories;

/// <summary>Consultas e comandos SQL parametrizados usados pelos controllers MVC.</summary>
public sealed class SqlServerRepository : DataConnection, ISIVADRepository
{
    public SqlServerRepository(IConfiguration configuration) : base(configuration) { }

    public async Task<List<Produto>> ListarProdutosAsync(bool somenteComEstoque = false)
    {
        const string sql = """
            SELECT p.codigo, p.codigo_barras, p.nome, p.preco_unit, p.estoque,
                   p.categoria_codigo, c.nome AS categoria_nome, p.cor,
                   COALESCE(ultimaCompra.fornecedor_nome, '') AS fornecedor_nome
            FROM Produtos p
            INNER JOIN Categorias c ON c.codigo = p.categoria_codigo
            OUTER APPLY
            (
                SELECT TOP (1) ce.fornecedor_nome
                FROM Itens_CompraEstoque ic
                INNER JOIN CompraEstoque ce ON ce.numero = ic.compra_numero AND ce.status = 1
                WHERE ic.produto_codigo = p.codigo
                ORDER BY ce.data_compra DESC, ce.numero DESC
            ) ultimaCompra
            WHERE p.ativo = 1 AND (@somenteComEstoque = 0 OR p.estoque > 0)
            ORDER BY p.nome, p.codigo;
            """;
        var produtos = new List<Produto>();
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@somenteComEstoque", SqlDbType.Bit).Value = somenteComEstoque;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            produtos.Add(new Produto
            {
                Codigo = reader.GetInt32(0), CodigoBarras = reader.GetString(1),
                Nome = reader.GetString(2), PrecoUnit = reader.GetDecimal(3),
                Estoque = reader.GetInt32(4), CategoriaCodigo = reader.GetInt32(5),
                Categoria = new Categoria { Codigo = reader.GetInt32(5), Nome = reader.GetString(6) },
                Cor = reader.GetString(7), FornecedorNome = reader.GetString(8)
            });
        }
        return produtos;
    }

    public async Task<List<Cliente>> ListarClientesAsync()
    {
        const string sql = """
            SELECT p.id, p.nome, c.cpf FROM Pessoas p
            INNER JOIN Clientes c ON c.pessoa_id = p.id ORDER BY p.nome;
            """;
        var lista = new List<Cliente>();
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            lista.Add(new Cliente { Id = reader.GetInt32(0), Nome = reader.GetString(1), CPF = reader.GetString(2) });
        return lista;
    }

    public async Task<int?> BuscarClienteIdPorCpfAsync(string cpf)
    {
        var digitos = new string((cpf ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digitos.Length != 11) return null;
        const string sql = """
            SELECT TOP (1) pessoa_id FROM Clientes
            WHERE REPLACE(REPLACE(REPLACE(cpf, '.', ''), '-', ''), ' ', '') = @cpf;
            """;
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@cpf", SqlDbType.VarChar, 11).Value = digitos;
        var result = await command.ExecuteScalarAsync();
        return result is null || result is DBNull ? null : Convert.ToInt32(result);
    }

    public async Task<List<Funcionario>> ListarFuncionariosAsync()
    {
        const string sql = """
            SELECT p.id, p.nome FROM Pessoas p
            INNER JOIN Funcionarios f ON f.pessoa_id = p.id WHERE f.ativo = 1 ORDER BY p.nome;
            """;
        var lista = new List<Funcionario>();
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) lista.Add(new Funcionario { Id = reader.GetInt32(0), Nome = reader.GetString(1) });
        return lista;
    }

    public async Task<List<Administrador>> ListarAdministradoresAsync()
    {
        const string sql = """
            SELECT p.id, p.nome FROM Pessoas p
            INNER JOIN Administradores a ON a.pessoa_id = p.id ORDER BY p.nome;
            """;
        var lista = new List<Administrador>();
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) lista.Add(new Administrador { Id = reader.GetInt32(0), Nome = reader.GetString(1) });
        return lista;
    }

    public async Task<List<Fornecedor>> ListarFornecedoresAsync()
    {
        const string sql = """
            SELECT p.id, p.nome, f.cnpj FROM Pessoas p
            INNER JOIN Fornecedores f ON f.pessoa_id = p.id ORDER BY p.nome;
            """;
        var lista = new List<Fornecedor>();
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) lista.Add(new Fornecedor { Id = reader.GetInt32(0), Nome = reader.GetString(1), CNPJ = reader.GetString(2) });
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
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            lista.Add(new Produto
            {
                Codigo = reader.GetInt32(0), CodigoBarras = reader.GetString(1), Nome = reader.GetString(2),
                PrecoUnit = reader.GetDecimal(3), Estoque = reader.GetInt32(4), CategoriaCodigo = reader.GetInt32(5),
                Categoria = new Categoria { Codigo = reader.GetInt32(5), Nome = reader.GetString(6) }
            });
        return lista;
    }

    public async Task<List<CompraEstoque>> ListarComprasRecentesAsync(int quantidade = 5)
    {
        const string sql = """
            SELECT TOP (@quantidade) c.numero, c.data_compra, c.total, c.status,
                   c.administrador_id, c.fornecedor_id, COALESCE(c.fornecedor_nome,p.nome) AS fornecedor_nome
            FROM CompraEstoque c
            LEFT JOIN Fornecedores f ON f.pessoa_id = c.fornecedor_id
            LEFT JOIN Pessoas p ON p.id = f.pessoa_id
            ORDER BY c.data_compra DESC, c.numero DESC;
            """;
        var lista = new List<CompraEstoque>();
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@quantidade", SqlDbType.Int).Value = quantidade;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new CompraEstoque
            {
                Numero = reader.GetInt32(0), DataCompra = reader.GetDateTime(1), Total = reader.GetDecimal(2),
                Status = reader.GetInt32(3), AdministradorId = reader.GetInt32(4),
                FornecedorId = reader.IsDBNull(5) ? null : reader.GetInt32(5), FornecedorNome = reader.GetString(6),
                Fornecedor = new Fornecedor { Nome = reader.GetString(6) }
            });
        }
        return lista;
    }

    public async Task<DashboardViewModel> ObterDashboardAsync()
    {
        var model = new DashboardViewModel();
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        const string totalsSql = """
            SELECT
              COALESCE((SELECT SUM(valor_total) FROM Pedidos WHERE status = 1), 0),
              COALESCE((SELECT SUM(valor_total) FROM Pedidos WHERE status = 1 AND YEAR(data_pedido)=YEAR(GETDATE()) AND MONTH(data_pedido)=MONTH(GETDATE())), 0),
              COALESCE((SELECT SUM(ip.qtd) FROM Itens_Pedidos ip INNER JOIN Pedidos p ON p.codigo=ip.pedido_codigo WHERE p.status=1), 0),
              (SELECT COUNT(*) FROM Pedidos WHERE status=2),
              (SELECT COUNT(*) FROM Produtos WHERE ativo=1 AND estoque<=10);
            """;
        await using (var command = new SqlCommand(totalsSql, connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                model.VendasTotais = reader.GetDecimal(0);
                model.VendasDoMes = reader.GetDecimal(1);
                model.UnidadesVendidas = Convert.ToInt32(reader.GetValue(2));
                model.PedidosCancelados = reader.GetInt32(3);
                model.AlertasEstoque = reader.GetInt32(4);
            }
        }

        const string salesSql = """
            WITH Meses(Mes) AS
            (
                SELECT Mes FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12)) AS Lista(Mes)
            ),
            Vendas AS
            (
                SELECT MONTH(data_pedido) AS Mes, SUM(valor_total) AS Total
                FROM Pedidos WHERE status=1 AND YEAR(data_pedido)=YEAR(GETDATE())
                GROUP BY MONTH(data_pedido)
            ),
            Compras AS
            (
                SELECT MONTH(data_compra) AS Mes, SUM(total) AS Total
                FROM CompraEstoque WHERE status=1 AND YEAR(data_compra)=YEAR(GETDATE())
                GROUP BY MONTH(data_compra)
            )
            SELECT Meses.Mes, COALESCE(Vendas.Total,0), COALESCE(Compras.Total,0)
            FROM Meses
            LEFT JOIN Vendas ON Vendas.Mes=Meses.Mes
            LEFT JOIN Compras ON Compras.Mes=Meses.Mes
            ORDER BY Meses.Mes;
            """;
        await using (var command = new SqlCommand(salesSql, connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            var culture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
            while (await reader.ReadAsync())
                model.VendasPorMes.Add(new MonthlySalesViewModel
                {
                    Label = culture.DateTimeFormat.GetAbbreviatedMonthName(reader.GetInt32(0)),
                    Total = reader.GetDecimal(1),
                    Compras = reader.GetDecimal(2)
                });
        }

        const string categorySql = """
            SELECT c.nome, SUM(ip.qtd) AS unidades
            FROM Itens_Pedidos ip
            INNER JOIN Pedidos pe ON pe.codigo=ip.pedido_codigo AND pe.status=1
            INNER JOIN Produtos pr ON pr.codigo=ip.produto_codigo
            INNER JOIN Categorias c ON c.codigo=pr.categoria_codigo
            GROUP BY c.nome ORDER BY unidades DESC;
            """;
        await using (var command = new SqlCommand(categorySql, connection))
        await using (var reader = await command.ExecuteReaderAsync())
            while (await reader.ReadAsync()) model.VendasPorCategoria.Add(new CategorySalesViewModel { Categoria = reader.GetString(0), Quantidade = reader.GetInt32(1) });

        const string activitiesSql = """
            SELECT TOP (5) Descricao, DataMovimento, Tipo
            FROM
            (
                SELECT CONCAT('Pedido #', codigo, CASE status WHEN 0 THEN ' pendente' WHEN 2 THEN ' cancelado' ELSE ' contabilizado' END) AS Descricao,
                       data_pedido AS DataMovimento,
                       CASE status WHEN 0 THEN 'pendente' WHEN 2 THEN 'cancelado' ELSE 'venda' END AS Tipo,
                       codigo AS Ordem
                FROM Pedidos
                UNION ALL
                SELECT CONCAT('Compra de estoque #', numero, ' recebida') AS Descricao,
                       data_compra AS DataMovimento, 'compra' AS Tipo, numero AS Ordem
                FROM CompraEstoque WHERE status = 1
            ) Atividades
            ORDER BY DataMovimento DESC, Ordem DESC;
            """;
        await using (var command = new SqlCommand(activitiesSql, connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                model.Atividades.Add(new ActivityViewModel
                {
                    Descricao = reader.GetString(0), Data = reader.GetDateTime(1), Tipo = reader.GetString(2)
                });
            }
        }
        const string lowStockSql = """
            SELECT TOP (5) p.codigo, p.codigo_barras, p.nome, p.preco_unit, p.estoque, p.categoria_codigo, c.nome
            FROM Produtos p INNER JOIN Categorias c ON c.codigo=p.categoria_codigo
            WHERE p.ativo = 1 AND p.estoque <= 10 ORDER BY p.estoque, p.nome;
            """;
        await using (var command = new SqlCommand(lowStockSql, connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) model.EstoqueBaixo.Add(new Produto
            {
                Codigo = reader.GetInt32(0), CodigoBarras = reader.GetString(1), Nome = reader.GetString(2),
                PrecoUnit = reader.GetDecimal(3), Estoque = reader.GetInt32(4), CategoriaCodigo = reader.GetInt32(5),
                Categoria = new Categoria { Codigo = reader.GetInt32(5), Nome = reader.GetString(6) }
            });
        }
        return model;
    }

    public async Task<List<ReportMovementViewModel>> ListarMovimentosRelatorioAsync()
    {
        const string sql = """
            SELECT data_pedido AS data_movimento, 'venda' AS tipo, valor_total AS valor
            FROM Pedidos WHERE status=1
            UNION ALL
            SELECT data_compra, 'despesa', total FROM CompraEstoque WHERE status=1
            ORDER BY data_movimento;
            """;
        var movimentos = new List<ReportMovementViewModel>();
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) movimentos.Add(new ReportMovementViewModel
        {
            Data = reader.GetDateTime(0), Tipo = reader.GetString(1), Valor = reader.GetDecimal(2)
        });
        return movimentos;
    }

    public async Task<int> RegistrarPedidoAsync(Pedido pedido)
    {
        if (pedido.Itens.Count == 0 || pedido.Itens.Any(item => item.Qtd <= 0))
            throw new InvalidOperationException("Informe pelo menos um produto com quantidade maior que zero.");
        if (pedido.Itens.GroupBy(item => item.ProdutoCodigo).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Cada produto deve aparecer uma única vez no pedido.");
        if (pedido.ClienteId <= 0 || pedido.FuncionarioId <= 0)
            throw new InvalidOperationException("O pedido precisa estar associado a um cliente e a um funcionário válidos.");

        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        decimal total = 0;
        foreach (var item in pedido.Itens)
        {
            await using var priceCommand = new SqlCommand("SELECT preco_unit,estoque FROM Produtos WHERE codigo=@codigo AND ativo=1", connection, transaction);
            AddInt(priceCommand, "@codigo", item.ProdutoCodigo);
            decimal price;
            int available;
            await using (var reader = await priceCommand.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync()) throw new InvalidOperationException($"O produto {item.ProdutoCodigo} não existe ou está inativo.");
                price = reader.GetDecimal(0);
                available = reader.GetInt32(1);
            }
            if (item.Qtd > available) throw new InvalidOperationException($"Estoque insuficiente para o produto {item.ProdutoCodigo}; disponível: {available}.");
            var line = price * item.Qtd;
            item.PrecoTotal = line;
            total += line;
        }
        const string insertSql = """
            INSERT INTO Pedidos (status, valor_total, cliente_id, funcionario_id, data_pedido)
            OUTPUT INSERTED.codigo
            VALUES (@status, @total, @cliente, @funcionario, GETDATE());
            """;
        await using (var command = new SqlCommand(insertSql, connection, transaction))
        {
            AddInt(command, "@status", 0);
            AddMoney(command, "@total", total);
            AddInt(command, "@cliente", pedido.ClienteId);
            AddInt(command, "@funcionario", pedido.FuncionarioId);
            pedido.Codigo = Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        foreach (var item in pedido.Itens)
        {
            await using var command = new SqlCommand("INSERT INTO Itens_Pedidos (pedido_codigo, produto_codigo, qtd, preco_total) VALUES (@pedido,@produto,@qtd,@total)", connection, transaction);
            AddInt(command, "@pedido", pedido.Codigo); AddInt(command, "@produto", item.ProdutoCodigo);
            AddInt(command, "@qtd", item.Qtd); AddMoney(command, "@total", item.PrecoTotal);
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
        pedido.ValorTotal = total;
        return pedido.Codigo;
    }

    public async Task<bool> AtualizarStatusPedidoAsync(int codigo, int status)
    {
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        int atual;
        await using (var get = new SqlCommand("SELECT status FROM Pedidos WITH (UPDLOCK, ROWLOCK) WHERE codigo=@codigo", connection, transaction))
        {
            AddInt(get, "@codigo", codigo);
            var value = await get.ExecuteScalarAsync();
            if (value is null) return false;
            atual = Convert.ToInt32(value);
        }
        if (atual == status) return false;
        if (status is not (1 or 2)) throw new InvalidOperationException("Status de pedido inválido.");
        if (status == 1 && atual != 0) return false;
        if (status == 2 && atual is not (0 or 1)) return false;
        if (status == 1 && atual == 0)
        {
            var items = await LerItensPedidoNaTransacaoAsync(connection, transaction, codigo);
            foreach (var item in items)
            {
                await using var update = new SqlCommand("UPDATE Produtos SET estoque=estoque-@qtd WHERE codigo=@produto AND estoque>=@qtd", connection, transaction);
                AddInt(update, "@qtd", item.Qtd); AddInt(update, "@produto", item.ProdutoCodigo);
                if (await update.ExecuteNonQueryAsync() == 0) throw new InvalidOperationException($"Estoque insuficiente para o produto {item.ProdutoCodigo}.");
            }
        }
        else if (status == 2 && atual == 1)
        {
            var items = await LerItensPedidoNaTransacaoAsync(connection, transaction, codigo);
            foreach (var item in items)
            {
                await using var update = new SqlCommand("UPDATE Produtos SET estoque=estoque+@qtd WHERE codigo=@produto", connection, transaction);
                AddInt(update, "@qtd", item.Qtd); AddInt(update, "@produto", item.ProdutoCodigo);
                await update.ExecuteNonQueryAsync();
            }
        }
        await using (var update = new SqlCommand("UPDATE Pedidos SET status=@status WHERE codigo=@codigo", connection, transaction))
        {
            AddInt(update, "@status", status); AddInt(update, "@codigo", codigo);
            await update.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
        return true;
    }

    public async Task<Pedido?> ObterPedidoAsync(int codigo)
    {
        const string sql = "SELECT codigo,status,valor_total,cliente_id,funcionario_id FROM Pedidos WHERE codigo=@codigo";
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        Pedido? pedido;
        await using (var command = new SqlCommand(sql, connection))
        {
            AddInt(command, "@codigo", codigo);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            pedido = new Pedido
            {
                Codigo = reader.GetInt32(0), Status = reader.GetInt32(1), ValorTotal = reader.GetDecimal(2),
                ClienteId = reader.GetInt32(3), FuncionarioId = reader.GetInt32(4)
            };
        }
        pedido.Itens = await LerItensPedidoAsync(connection, codigo);
        return pedido;
    }

    public async Task<Pedido?> ObterPrimeiroPedidoAsync()
    {
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        int? codigo;
        await using (var command = new SqlCommand("SELECT TOP (1) codigo FROM Pedidos ORDER BY codigo", connection))
        {
            var result = await command.ExecuteScalarAsync();
            codigo = result is null ? null : Convert.ToInt32(result);
        }
        return codigo.HasValue ? await ObterPedidoAsync(codigo.Value) : null;
    }

    public async Task<int> RegistrarCompraEstoqueAsync(CompraEstoque compra)
    {
        if (string.IsNullOrWhiteSpace(compra.FornecedorNome) || compra.FornecedorNome.Trim().Length > 150)
            throw new InvalidOperationException("Informe o nome do fornecedor (até 150 caracteres).");
        if (compra.AdministradorId <= 0 || compra.Itens.Count == 0 || compra.Itens.Any(item => item.Quantidade <= 0 || item.PrecoUnitario < 0))
            throw new InvalidOperationException("Informe um administrador, ao menos um item, quantidade positiva e preço válido.");
        if (compra.Itens.GroupBy(item => item.ProdutoCodigo).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Cada produto deve aparecer uma única vez na compra.");
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        var total = compra.Itens.Sum(item => item.PrecoUnitario * item.Quantidade);
        const string insertSql = """
            INSERT INTO CompraEstoque(data_compra,total,status,administrador_id,fornecedor_id,fornecedor_nome)
            OUTPUT INSERTED.numero VALUES(COALESCE(@data,GETDATE()),@total,1,@administrador,NULL,@fornecedorNome);
            """;
        await using (var command = new SqlCommand(insertSql, connection, transaction))
        {
            command.Parameters.Add("@data", SqlDbType.DateTime).Value = compra.DataCompra == default ? DBNull.Value : compra.DataCompra;
            AddMoney(command, "@total", total); AddInt(command, "@administrador", compra.AdministradorId);
            command.Parameters.Add("@fornecedorNome", SqlDbType.VarChar, 150).Value = compra.FornecedorNome.Trim();
            compra.Numero = Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        foreach (var item in compra.Itens)
        {
            var lineTotal = item.PrecoUnitario * item.Quantidade;
            var corInformada = string.IsNullOrWhiteSpace(item.Cor) ? string.Empty : item.Cor.Trim();
            if (corInformada.Length > 60) throw new InvalidOperationException("A cor do produto deve ter até 60 caracteres.");
            await using (var update = new SqlCommand("UPDATE Produtos SET estoque=estoque+@qtd,cor=CASE WHEN @cor='' THEN cor ELSE @cor END OUTPUT INSERTED.cor WHERE codigo=@produto AND ativo=1", connection, transaction))
            {
                AddInt(update, "@qtd", item.Quantidade); AddInt(update, "@produto", item.ProdutoCodigo);
                update.Parameters.Add("@cor", SqlDbType.VarChar, 60).Value = corInformada;
                var corSalva = await update.ExecuteScalarAsync();
                if (corSalva is null || corSalva is DBNull) throw new InvalidOperationException($"O produto {item.ProdutoCodigo} não existe ou está inativo.");
                item.Cor = Convert.ToString(corSalva) ?? "Não informada";
            }
            await using var insert = new SqlCommand("INSERT INTO Itens_CompraEstoque(compra_numero,produto_codigo,cor,quantidade,valor) VALUES(@compra,@produto,@cor,@qtd,@total)", connection, transaction);
            AddInt(insert, "@compra", compra.Numero); AddInt(insert, "@produto", item.ProdutoCodigo);
            insert.Parameters.Add("@cor", SqlDbType.VarChar, 60).Value = item.Cor;
            AddInt(insert, "@qtd", item.Quantidade); AddMoney(insert, "@total", lineTotal);
            await insert.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
        compra.Total = total;
        compra.Status = 1;
        return compra.Numero;
    }

    public async Task<CompraEstoque?> ObterPrimeiraCompraEstoqueAsync()
    {
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        int? numero;
        await using (var command = new SqlCommand("SELECT TOP (1) numero FROM CompraEstoque ORDER BY numero", connection))
        {
            var result = await command.ExecuteScalarAsync();
            numero = result is null ? null : Convert.ToInt32(result);
        }
        if (!numero.HasValue) return null;
        await using var header = new SqlCommand("SELECT numero,data_compra,total,status,administrador_id,fornecedor_id,fornecedor_nome FROM CompraEstoque WHERE numero=@numero", connection);
        AddInt(header, "@numero", numero.Value);
        CompraEstoque compra;
        await using (var reader = await header.ExecuteReaderAsync())
        {
            if (!await reader.ReadAsync()) return null;
            compra = new CompraEstoque
            {
                Numero = reader.GetInt32(0), DataCompra = reader.GetDateTime(1), Total = reader.GetDecimal(2),
                Status = reader.GetInt32(3), AdministradorId = reader.GetInt32(4),
                FornecedorId = reader.IsDBNull(5) ? null : reader.GetInt32(5), FornecedorNome = reader.GetString(6)
            };
        }
        compra.Itens = await LerItensCompraAsync(connection, compra.Numero);
        return compra;
    }

    private static async Task<List<ItemPedido>> LerItensPedidoAsync(SqlConnection connection, int codigo)
    {
        const string sql = """
            SELECT i.pedido_codigo,i.produto_codigo,i.qtd,i.preco_total,p.nome,p.codigo_barras,p.estoque,p.categoria_codigo,c.nome
            FROM Itens_Pedidos i INNER JOIN Produtos p ON p.codigo=i.produto_codigo
            INNER JOIN Categorias c ON c.codigo=p.categoria_codigo WHERE i.pedido_codigo=@codigo ORDER BY p.nome;
            """;
        var items = new List<ItemPedido>();
        await using var command = new SqlCommand(sql, connection); AddInt(command, "@codigo", codigo);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) items.Add(new ItemPedido
        {
            PedidoCodigo = reader.GetInt32(0), ProdutoCodigo = reader.GetInt32(1), Qtd = reader.GetInt32(2), PrecoTotal = reader.GetDecimal(3),
            Produto = new Produto
            {
                Codigo = reader.GetInt32(1), Nome = reader.GetString(4), CodigoBarras = reader.GetString(5),
                Estoque = reader.GetInt32(6), CategoriaCodigo = reader.GetInt32(7), Categoria = new Categoria { Codigo = reader.GetInt32(7), Nome = reader.GetString(8) }
            }
        });
        return items;
    }

    private static async Task<List<ItemPedido>> LerItensPedidoNaTransacaoAsync(SqlConnection connection, SqlTransaction transaction, int codigo)
    {
        var items = new List<ItemPedido>();
        await using var command = new SqlCommand("SELECT produto_codigo,qtd FROM Itens_Pedidos WHERE pedido_codigo=@codigo", connection, transaction);
        AddInt(command, "@codigo", codigo);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) items.Add(new ItemPedido { ProdutoCodigo = reader.GetInt32(0), Qtd = reader.GetInt32(1) });
        return items;
    }

    private static async Task<List<ItemCompraEstoque>> LerItensCompraAsync(SqlConnection connection, int numero)
    {
        var items = new List<ItemCompraEstoque>();
        await using var command = new SqlCommand("SELECT compra_numero,produto_codigo,quantidade,valor,cor FROM Itens_CompraEstoque WHERE compra_numero=@numero", connection);
        AddInt(command, "@numero", numero);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) items.Add(new ItemCompraEstoque
        {
            CompraNumero = reader.GetInt32(0), ProdutoCodigo = reader.GetInt32(1), Quantidade = reader.GetInt32(2), Valor = reader.GetDecimal(3), Cor = reader.GetString(4)
        });
        return items;
    }

    public async Task<bool> AtualizarProdutoAsync(Produto produto)
    {
        if (produto.Codigo <= 0 || string.IsNullOrWhiteSpace(produto.CodigoBarras) || string.IsNullOrWhiteSpace(produto.Nome) || produto.PrecoUnit < 0 || produto.Estoque < 0 || produto.CategoriaCodigo <= 0 || (produto.Cor?.Trim().Length ?? 0) > 60)
            throw new InvalidOperationException("Informe código, nome, cor (até 60 caracteres), preço, quantidade e categoria válidos.");
        produto.Cor = string.IsNullOrWhiteSpace(produto.Cor) ? "Não informada" : produto.Cor.Trim();
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        const string sql = "UPDATE Produtos SET codigo_barras=@barcode,nome=@name,cor=@cor,preco_unit=@price,estoque=@stock,categoria_codigo=@category WHERE codigo=@id AND ativo=1";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@barcode", SqlDbType.VarChar, 50).Value = produto.CodigoBarras.Trim();
        command.Parameters.Add("@name", SqlDbType.VarChar, 150).Value = produto.Nome.Trim();
        command.Parameters.Add("@cor", SqlDbType.VarChar, 60).Value = produto.Cor;
        AddMoney(command, "@price", produto.PrecoUnit); AddInt(command, "@stock", produto.Estoque);
        AddInt(command, "@category", produto.CategoriaCodigo); AddInt(command, "@id", produto.Codigo);
        try { return await command.ExecuteNonQueryAsync() == 1; }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        { throw new InvalidOperationException("Já existe outro produto com esse código de barras.", ex); }
    }

    public async Task<bool> DesativarProdutoAsync(int codigo)
    {
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var command = new SqlCommand("UPDATE Produtos SET ativo=0 WHERE codigo=@codigo AND ativo=1", connection);
        AddInt(command, "@codigo", codigo);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<int> CadastrarFuncionarioAsync(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 150)
            throw new InvalidOperationException("Informe um nome de funcionário (até 150 caracteres).");
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        int id;
        await using (var insertPerson = new SqlCommand("INSERT INTO Pessoas(nome) OUTPUT INSERTED.id VALUES(@nome)", connection, transaction))
        {
            insertPerson.Parameters.Add("@nome", SqlDbType.VarChar, 150).Value = nome.Trim();
            id = Convert.ToInt32(await insertPerson.ExecuteScalarAsync());
        }
        await using (var insertEmployee = new SqlCommand("INSERT INTO Funcionarios(pessoa_id,ativo) VALUES(@id,1)", connection, transaction))
        {
            AddInt(insertEmployee, "@id", id);
            await insertEmployee.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
        return id;
    }

    public async Task<bool> DesativarFuncionarioAsync(int id)
    {
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var command = new SqlCommand("UPDATE Funcionarios SET ativo=0 WHERE pessoa_id=@id AND ativo=1", connection);
        AddInt(command, "@id", id);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> AtualizarFuncionarioAsync(int id, string nome)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 150)
            throw new InvalidOperationException("Informe um nome de funcionário (até 150 caracteres).");
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        const string sql = "UPDATE Pessoas SET nome=@nome FROM Pessoas INNER JOIN Funcionarios ON Funcionarios.pessoa_id=Pessoas.id WHERE Pessoas.id=@id AND Funcionarios.ativo=1";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@nome", SqlDbType.VarChar, 150).Value = nome.Trim();
        AddInt(command, "@id", id);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<List<Categoria>> ListarCategoriasAsync()
    {
        var lista = new List<Categoria>();
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT codigo,nome FROM Categorias ORDER BY nome", connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) lista.Add(new Categoria { Codigo = reader.GetInt32(0), Nome = reader.GetString(1) });
        return lista;
    }

    public async Task<int> CadastrarProdutoAsync(Produto produto)
    {
        if (string.IsNullOrWhiteSpace(produto.CodigoBarras) || string.IsNullOrWhiteSpace(produto.Nome) || produto.PrecoUnit < 0 || produto.Estoque < 0 || produto.CategoriaCodigo <= 0 || (produto.Cor?.Trim().Length ?? 0) > 60)
            throw new InvalidOperationException("Informe código de barras, nome, cor (até 60 caracteres), preço, quantidade e categoria válidos.");
        produto.Cor = string.IsNullOrWhiteSpace(produto.Cor) ? "Não informada" : produto.Cor.Trim();
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        const string sql = "INSERT INTO Produtos(codigo_barras,nome,cor,preco_unit,estoque,categoria_codigo) OUTPUT INSERTED.codigo VALUES(@barcode,@name,@cor,@price,@stock,@category)";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@barcode", SqlDbType.VarChar, 50).Value = produto.CodigoBarras.Trim();
        command.Parameters.Add("@name", SqlDbType.VarChar, 150).Value = produto.Nome.Trim();
        command.Parameters.Add("@cor", SqlDbType.VarChar, 60).Value = produto.Cor;
        AddMoney(command, "@price", produto.PrecoUnit);
        AddInt(command, "@stock", produto.Estoque);
        AddInt(command, "@category", produto.CategoriaCodigo);
        try
        {
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("Já existe um produto com esse código de barras.", ex);
        }
    }

    public async Task<DatabaseConnectionInfoViewModel> ObterDiagnosticoBancoAsync()
    {
        await using var connection = CriarConexao();
        await connection.OpenAsync();
        const string sql = "SELECT CAST(SERVERPROPERTY('ServerName') AS nvarchar(128)), DB_NAME(), (SELECT COUNT(*) FROM Produtos), (SELECT COUNT(*) FROM Funcionarios WHERE ativo=1), (SELECT COUNT(*) FROM Pedidos), (SELECT COUNT(*) FROM CompraEstoque)";
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("O SQL Server não retornou informações do banco conectado.");
        return new DatabaseConnectionInfoViewModel
        {
            Servidor = reader.GetString(0), Banco = reader.GetString(1), Produtos = reader.GetInt32(2),
            FuncionariosAtivos = reader.GetInt32(3), Pedidos = reader.GetInt32(4), Compras = reader.GetInt32(5)
        };
    }

    private static void AddInt(SqlCommand command, string name, int value) => command.Parameters.Add(name, SqlDbType.Int).Value = value;
    private static void AddMoney(SqlCommand command, string name, decimal value)
    {
        var parameter = command.Parameters.Add(name, SqlDbType.Decimal);
        parameter.Precision = 10; parameter.Scale = 2; parameter.Value = value;
    }
}
