using System.Data;
using Microsoft.Data.SqlClient;
using SIVAD.Models;
using SIVAD.Models.ViewModels;

namespace SIVAD.Repositories;

/// <summary>Indicadores, gráficos, atividades, alertas e relatórios do painel administrativo.</summary>
public sealed partial class RepositorioSqlServer
{
    // Painel administrativo (dashboard): uma consulta de totais alimenta os cartões superiores;
    // devoluções são somadas por quantidade, não confundidas com pedidos cancelados.
    public async Task<DashboardViewModel> ObterDashboardAsync()
    {
        var modelo = new DashboardViewModel();
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        const string sqlTotais = """
            SELECT
              COALESCE((SELECT SUM(valor_total) FROM Pedidos WHERE status = 1), 0),
              COALESCE((SELECT SUM(valor_total) FROM Pedidos WHERE status = 1 AND YEAR(data_pedido)=YEAR(GETDATE()) AND MONTH(data_pedido)=MONTH(GETDATE())), 0),
              COALESCE((SELECT SUM(ip.qtd) FROM Itens_Pedidos ip INNER JOIN Pedidos p ON p.codigo=ip.pedido_codigo WHERE p.status=1), 0),
              COALESCE((SELECT SUM(quantidade) FROM Devolucoes), 0),
              (SELECT COUNT(*) FROM Produtos WHERE ativo=1 AND estoque<=10);
            """;
        await using (var comando = new SqlCommand(sqlTotais, conexao))
        await using (var leitor = await comando.ExecuteReaderAsync())
        {
            if (await leitor.ReadAsync())
            {
                modelo.VendasTotais = leitor.GetDecimal(0);
                modelo.VendasDoMes = leitor.GetDecimal(1);
                modelo.UnidadesVendidas = Convert.ToInt32(leitor.GetValue(2));
                modelo.UnidadesDevolvidas = Convert.ToInt32(leitor.GetValue(3));
                modelo.AlertasEstoque = leitor.GetInt32(4);
            }
        }

        const string sqlVendas = """
            SELECT MONTH(data_pedido) AS Mes, 1 AS Tipo, SUM(valor_total) AS Total
            FROM Pedidos
            WHERE status=1 AND YEAR(data_pedido)=YEAR(GETDATE())
            GROUP BY MONTH(data_pedido)
            UNION ALL
            SELECT MONTH(data_compra), 2, SUM(total)
            FROM CompraEstoque
            WHERE status=1 AND YEAR(data_compra)=YEAR(GETDATE())
            GROUP BY MONTH(data_compra)
            ORDER BY Mes, Tipo;
            """;
        // O SQL devolve totais agrupados. Os valores iniciais zero preservam
        // os meses sem movimento para que o gráfico sempre mostre o ano inteiro.
        var vendasPorMes = new decimal[12];
        var comprasPorMes = new decimal[12];
        await using (var comando = new SqlCommand(sqlVendas, conexao))
        await using (var leitor = await comando.ExecuteReaderAsync())
        {
            while (await leitor.ReadAsync())
            {
                var mes = leitor.GetInt32(0);
                var tipo = leitor.GetInt32(1);
                var total = leitor.GetDecimal(2);
                if (tipo == 1) vendasPorMes[mes - 1] = total;
                else comprasPorMes[mes - 1] = total;
            }
        }
        var cultura = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
        for (var mes = 1; mes <= 12; mes++)
            modelo.VendasPorMes.Add(new VendaMensalViewModel
            {
                Label = cultura.DateTimeFormat.GetAbbreviatedMonthName(mes),
                Total = vendasPorMes[mes - 1],
                Compras = comprasPorMes[mes - 1]
            });

        const string sqlComprasPorAno = "SELECT YEAR(data_compra), SUM(total) FROM CompraEstoque WHERE status=1 GROUP BY YEAR(data_compra) ORDER BY YEAR(data_compra)";
        await using (var comando = new SqlCommand(sqlComprasPorAno, conexao))
        await using (var leitor = await comando.ExecuteReaderAsync())
        {
            while (await leitor.ReadAsync())
                modelo.ComprasPorAno.Add(new CompraAnualViewModel
                {
                    Ano = leitor.GetInt32(0),
                    Total = leitor.GetDecimal(1)
                });
        }

        const string sqlCategorias = """
            SELECT c.nome, SUM(ip.qtd) AS unidades
            FROM Itens_Pedidos ip
            INNER JOIN Pedidos pe ON pe.codigo=ip.pedido_codigo AND pe.status=1
            INNER JOIN Produtos pr ON pr.codigo=ip.produto_codigo
            INNER JOIN Categorias c ON c.codigo=pr.categoria_codigo
            GROUP BY c.nome ORDER BY unidades DESC;
            """;
        await using (var comando = new SqlCommand(sqlCategorias, conexao))
        await using (var leitor = await comando.ExecuteReaderAsync())
            while (await leitor.ReadAsync()) modelo.VendasPorCategoria.Add(new VendaPorCategoriaViewModel { Categoria = leitor.GetString(0), Quantidade = leitor.GetInt32(1) });

        const string sqlAtividades = """
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
        await using (var comando = new SqlCommand(sqlAtividades, conexao))
        await using (var leitor = await comando.ExecuteReaderAsync())
        {
            while (await leitor.ReadAsync())
            {
                modelo.Atividades.Add(new AtividadeViewModel
                {
                    Descricao = leitor.GetString(0),
                    Data = leitor.GetDateTime(1),
                    Tipo = leitor.GetString(2)
                });
            }
        }
        const string sqlEstoqueBaixo = """
            SELECT TOP (5) p.codigo, p.codigo_barras, p.nome, p.preco_unit, p.estoque, p.categoria_codigo, c.nome
            FROM Produtos p INNER JOIN Categorias c ON c.codigo=p.categoria_codigo
            WHERE p.ativo = 1 AND p.estoque <= 10 ORDER BY p.estoque, p.nome;
            """;
        await using (var comando = new SqlCommand(sqlEstoqueBaixo, conexao))
        await using (var leitor = await comando.ExecuteReaderAsync())
        {
            while (await leitor.ReadAsync()) modelo.EstoqueBaixo.Add(new Produto
            {
                Codigo = leitor.GetInt32(0),
                CodigoBarras = leitor.GetString(1),
                Nome = leitor.GetString(2),
                PrecoUnit = leitor.GetDecimal(3),
                Estoque = leitor.GetInt32(4),
                CategoriaCodigo = leitor.GetInt32(5),
                Categoria = new Categoria { Codigo = leitor.GetInt32(5), Nome = leitor.GetString(6) }
            });
        }
        return modelo;
    }

    public async Task<List<MovimentoRelatorioViewModel>> ListarMovimentosRelatorioAsync()
    {
        const string sql = """
            SELECT data_pedido AS data_movimento, 'venda' AS tipo, valor_total AS valor, CAST(0 AS int) AS quantidade
            FROM Pedidos WHERE status=1
            UNION ALL
            SELECT data_compra, 'despesa', total, CAST(0 AS int) FROM CompraEstoque WHERE status=1
            UNION ALL
            SELECT data_devolucao, 'devolucao', valor_total, quantidade FROM Devolucoes
            ORDER BY data_movimento;
            """;
        var movimentos = new List<MovimentoRelatorioViewModel>();
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync()) movimentos.Add(new MovimentoRelatorioViewModel
        {
            Data = leitor.GetDateTime(0),
            Tipo = leitor.GetString(1),
            Valor = leitor.GetDecimal(2),
            Quantidade = leitor.GetInt32(3)
        });
        return movimentos;
    }
}
