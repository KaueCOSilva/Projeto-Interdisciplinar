USE SQLVendas2026
-------------------------------------------------------------------------
------------------------views tabelas de pessoas-------------------------
GO
ALTER VIEW vw_Administradores
AS
SELECT a.pessoa_id, p.nome, a.cpf, a.email, a.usuario, a.senha
FROM Administradores a
INNER JOIN Pessoas p
    ON p.pessoa_id = a.pessoa_id
GO
-------------------------------------------------------------------------
GO
ALTER VIEW vw_Funcionarios
AS
SELECT f.pessoa_id, p.nome, f.usuario, f.senha, f.ativo
FROM Funcionarios f
INNER JOIN Pessoas p
ON f.pessoa_id = p.pessoa_id
GO
-------------------------------------------------------------------------
GO
ALTER VIEW vw_clientes
AS
SELECT c.pessoa_id, p.nome, c.cpf
FROM Clientes c
INNER JOIN Pessoas p
ON c.pessoa_id = p.pessoa_id
GO
-------------------------------------------------------------------------
GO
ALTER VIEW vw_fornecedores
AS
SELECT fo.pessoa_id, p.nome, fo.cnpj
FROM Fornecedores fo 
INNER JOIN Pessoas p
ON fo.pessoa_id = p.pessoa_id
GO
-------------------------------------------------------------------------
------------------------views tabelas de operação------------------------
GO
CREATE VIEW vw_categorias
AS
SELECT *
FROM Categorias 
GO
-------------------------------------------------------------------------
GO
CREATE VIEW vw_produtos
AS
SELECT * 
FROM Produtos
GO
-------------------------------------------------------------------------
GO
CREATE VIEW vw_compraEstoque
AS
SELECT *
FROM CompraEstoque
GO
-------------------------------------------------------------------------
GO
CREATE VIEW vw_pedidos
AS
SELECT *
FROM Pedidos
GO
-------------------------------------------------------------------------
---------------------------views relacionamentos-------------------------
GO
CREATE VIEW vw_item_compraEstoque
AS
SELECT *
FROM Itens_CompraEstoque
GO
-------------------------------------------------------------------------
GO
CREATE VIEW vw_itensPedido
AS
SELECT *
FROM Itens_Pedidos
GO
-------------------------------------------------------------------------
GO
CREATE VIEW vw_devolucoes
AS
SELECT *
FROM Devolucoes
GO
-------------------------------------------------------------------------
------------------------Views com uso de Joins---------------------------

----view produto com categoria----
--Seleciona info de produto com um inner join em categoria
--Usa um OUTER APPLY para buscar, pra cada linha do select,
--O nome do fornecedor responsável pelo produto.
--Caso nulo, retorna ''
CREATE VIEW vw_produto_categoria
AS
SELECT
    p.produto_codigo,
    p.codigo_barras,
    p.nome,
    p.preco_unit,
    p.estoque,
    c.categoria_codigo,
    c.nome AS categoria,
    p.cor,
    COALESCE(ultimaCompra.fornecedor_nome, '')
        AS fornecedor_nome,
    p.ativo
FROM Produtos p
INNER JOIN Categorias c 
    ON c.categoria_codigo = p.categoria_codigo 
OUTER APPLY
(
    SELECT TOP (1) ce.fornecedor_nome
    FROM Itens_CompraEstoque ice
    INNER JOIN CompraEstoque ce
        ON ce.numero = ice.compra_numero
        AND ce.status = 1
    WHERE ice.produto_codigo = p.produto_codigo

) ultimaCompra
-------------------------------------------------------------------------
------------------------View Funcionário Ativo---------------------------
-----------------------Não considerar como Join--------------------------
GO
create VIEW vw_funcionarios_ativos
AS
SELECT p.pessoa_id, p.nome
FROM Pessoas p
INNER JOIN Funcionarios f
    ON F.pessoa_id = P.pessoa_id
WHERE f.ativo = 1
GO
-------------------------------------------------------------------------
--view estoque baixo
GO
CREATE VIEW vw_estoque_baixo
as
SELECT
    p.produto_codigo,
    p.codigo_barras,
    p.nome,
    p.preco_unit,
    p.estoque,
    p.categoria_codigo,
    c.nome AS categoria_nome
FROM Produtos p
INNER JOIN Categorias c
    ON c.categoria_codigo = p.categoria_codigo
WHERE
    p.ativo = 1
    AND p.estoque <= 10
GO
-------------------------------------------------------------------------
--view vendas por categoria
CREATE VIEW vw_VendasPorCategoria
AS
SELECT
    c.nome AS categoria_nome,
    SUM(ip.qtd) AS unidades
FROM Itens_Pedidos ip
INNER JOIN Pedidos pe
    ON pe.pedido_codigo = ip.pedido_codigo
   AND pe.status = 1
INNER JOIN Produtos pr
    ON pr.produto_codigo = ip.produto_codigo
INNER JOIN Categorias c
    ON c.categoria_codigo = pr.categoria_codigo
GROUP BY
    c.nome;
-------------------------------------------------------------------------
--view ItemPedidoDetalhadao
GO
CREATE VIEW vw_ItensPedidosDetalhados
AS
SELECT
    i.pedido_codigo,
    i.produto_codigo,
    i.qtd,
    i.preco_total,
    p.nome AS produto_nome,
    p.codigo_barras,
    p.estoque,
    p.categoria_codigo,
    c.nome AS categoria_nome
FROM Itens_Pedidos i
INNER JOIN Produtos p
    ON p.produto_codigo = i.produto_codigo
INNER JOIN Categorias c
    ON c.categoria_codigo = p.categoria_codigo;
--------------------------------------------------------------------------

