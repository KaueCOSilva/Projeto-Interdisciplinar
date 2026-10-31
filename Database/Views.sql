/* SIVAD — Views documentadas. Execute depois de Banco.sql/AtualizarBanco.sql. */
USE SQLVendas2026;
GO

CREATE OR ALTER VIEW dbo.vw_Administradores AS
SELECT a.pessoa_id, p.nome, a.cpf, a.email
FROM dbo.Administradores a INNER JOIN dbo.Pessoas p ON p.id = a.pessoa_id;
GO
CREATE OR ALTER VIEW dbo.vw_Funcionarios AS
SELECT f.pessoa_id, p.nome, f.ativo
FROM dbo.Funcionarios f INNER JOIN dbo.Pessoas p ON p.id = f.pessoa_id;
GO
CREATE OR ALTER VIEW dbo.vw_clientes AS
SELECT c.pessoa_id, p.nome, c.cpf
FROM dbo.Clientes c INNER JOIN dbo.Pessoas p ON p.id = c.pessoa_id;
GO
CREATE OR ALTER VIEW dbo.vw_fornecedores AS
SELECT f.pessoa_id, p.nome, f.cnpj
FROM dbo.Fornecedores f INNER JOIN dbo.Pessoas p ON p.id = f.pessoa_id;
GO
CREATE OR ALTER VIEW dbo.vw_categorias AS
SELECT codigo, nome FROM dbo.Categorias;
GO
CREATE OR ALTER VIEW dbo.vw_produtos AS
SELECT codigo, codigo_barras, nome, cor, preco_unit, estoque, categoria_codigo, ativo
FROM dbo.Produtos;
GO
CREATE OR ALTER VIEW dbo.vw_compraEstoque AS
SELECT numero, data_compra, total, status, administrador_id, fornecedor_id, fornecedor_nome
FROM dbo.CompraEstoque;
GO
CREATE OR ALTER VIEW dbo.vw_pedidos AS
SELECT codigo, status, valor_total, data_pedido, cliente_id, funcionario_id
FROM dbo.Pedidos;
GO
CREATE OR ALTER VIEW dbo.vw_item_compraEstoque AS
SELECT compra_numero, produto_codigo, cor, quantidade, valor
FROM dbo.Itens_CompraEstoque;
GO
CREATE OR ALTER VIEW dbo.vw_itensPedido AS
SELECT pedido_codigo, produto_codigo, qtd, preco_total
FROM dbo.Itens_Pedidos;
GO
CREATE OR ALTER VIEW dbo.vw_devolucoes AS
SELECT numero, data_devolucao, produto_codigo, quantidade, valor_total
FROM dbo.Devolucoes;
GO

CREATE OR ALTER VIEW dbo.vw_produto_categoria AS
SELECT p.codigo AS produto_codigo, p.codigo_barras, p.nome, p.preco_unit,
       p.estoque, c.codigo AS categoria_codigo, c.nome AS categoria,
       p.cor, COALESCE(ultimaCompra.fornecedor_nome, '') AS fornecedor_nome, p.ativo
FROM dbo.Produtos p
INNER JOIN dbo.Categorias c ON c.codigo = p.categoria_codigo
OUTER APPLY (
    SELECT TOP (1) ce.fornecedor_nome
    FROM dbo.Itens_CompraEstoque ice
    INNER JOIN dbo.CompraEstoque ce ON ce.numero = ice.compra_numero AND ce.status = 1
    WHERE ice.produto_codigo = p.codigo
    ORDER BY ce.data_compra DESC, ce.numero DESC
) ultimaCompra;
GO

CREATE OR ALTER VIEW dbo.vw_funcionarios_ativos AS
SELECT p.id AS pessoa_id, p.nome
FROM dbo.Pessoas p INNER JOIN dbo.Funcionarios f ON f.pessoa_id = p.id
WHERE f.ativo = 1;
GO
CREATE OR ALTER VIEW dbo.vw_estoque_baixo AS
SELECT p.codigo AS produto_codigo, p.codigo_barras, p.nome, p.preco_unit,
       p.estoque, p.categoria_codigo, c.nome AS categoria_nome
FROM dbo.Produtos p INNER JOIN dbo.Categorias c ON c.codigo = p.categoria_codigo
WHERE p.ativo = 1 AND p.estoque <= 10;
GO

CREATE OR ALTER VIEW dbo.vw_VendasPorCategoria AS
SELECT c.nome AS categoria_nome, SUM(ip.qtd) AS unidades
FROM dbo.Itens_Pedidos ip
INNER JOIN dbo.Pedidos pe ON pe.codigo = ip.pedido_codigo AND pe.status = 1
INNER JOIN dbo.Produtos pr ON pr.codigo = ip.produto_codigo
INNER JOIN dbo.Categorias c ON c.codigo = pr.categoria_codigo
GROUP BY c.nome;
GO
CREATE OR ALTER VIEW dbo.vw_ItensPedidosDetalhados AS
SELECT i.pedido_codigo, i.produto_codigo, i.qtd, i.preco_total,
       p.nome AS produto_nome, p.codigo_barras, p.estoque,
       p.categoria_codigo, c.nome AS categoria_nome
FROM dbo.Itens_Pedidos i
INNER JOIN dbo.Produtos p ON p.codigo = i.produto_codigo
INNER JOIN dbo.Categorias c ON c.codigo = p.categoria_codigo;
GO
CREATE OR ALTER VIEW dbo.vw_ComprasEstoqueDetalhadas AS
SELECT c.numero, c.data_compra, c.total, c.status, c.administrador_id,
       c.fornecedor_id, COALESCE(c.fornecedor_nome, p.nome, '') AS fornecedor_nome
FROM dbo.CompraEstoque c
LEFT JOIN dbo.Fornecedores f ON f.pessoa_id = c.fornecedor_id
LEFT JOIN dbo.Pessoas p ON p.id = f.pessoa_id;
GO

CREATE OR ALTER VIEW dbo.vw_VendasComprasMensais AS
WITH Meses(Mes) AS (
    SELECT Mes FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12)) Lista(Mes)
), Vendas AS (
    SELECT MONTH(data_pedido) AS Mes, SUM(valor_total) AS Total
    FROM dbo.Pedidos
    WHERE status = 1 AND YEAR(data_pedido) = YEAR(GETDATE())
    GROUP BY MONTH(data_pedido)
), Compras AS (
    SELECT MONTH(data_compra) AS Mes, SUM(total) AS Total
    FROM dbo.CompraEstoque
    WHERE status = 1 AND YEAR(data_compra) = YEAR(GETDATE())
    GROUP BY MONTH(data_compra)
)
SELECT Meses.Mes, COALESCE(Vendas.Total, 0) AS TotalVendas,
       COALESCE(Compras.Total, 0) AS TotalCompras
FROM Meses
LEFT JOIN Vendas ON Vendas.Mes = Meses.Mes
LEFT JOIN Compras ON Compras.Mes = Meses.Mes;
GO

CREATE OR ALTER VIEW dbo.vw_CredenciaisAcesso AS
SELECT c.pessoa_id, p.nome, c.perfil, c.email, c.ativo, c.data_criacao
FROM dbo.CredenciaisAcesso c INNER JOIN dbo.Pessoas p ON p.id = c.pessoa_id;
GO
CREATE OR ALTER VIEW dbo.vw_SolicitacoesRecuperacaoSenha AS
SELECT s.solicitacao_id, s.pessoa_id, p.nome AS pessoa_nome, c.email,
       s.data_solicitacao, s.status
FROM dbo.SolicitacoesRecuperacaoSenha s
INNER JOIN dbo.CredenciaisAcesso c ON c.pessoa_id = s.pessoa_id
INNER JOIN dbo.Pessoas p ON p.id = s.pessoa_id;
GO

SELECT name AS view_criada
FROM sys.views
WHERE name IN ('vw_Administradores','vw_Funcionarios','vw_clientes','vw_fornecedores',
'vw_categorias','vw_produtos','vw_compraEstoque','vw_pedidos','vw_item_compraEstoque',
'vw_itensPedido','vw_devolucoes','vw_produto_categoria','vw_funcionarios_ativos',
'vw_estoque_baixo','vw_VendasPorCategoria','vw_ItensPedidosDetalhados',
'vw_ComprasEstoqueDetalhadas','vw_VendasComprasMensais','vw_CredenciaisAcesso',
'vw_SolicitacoesRecuperacaoSenha');
GO
