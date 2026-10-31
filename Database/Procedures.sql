/*
    SIVAD — Procedures do documento.
    As definitions são instaladas, mas nenhum EXEC de teste é disparado automaticamente.
    O sistema MVC mantém suas transações próprias no Repository; estas procedures ficam
    disponíveis para demonstração e administração pelo SQL Client.
*/
USE SQLVendas2026;
GO

CREATE OR ALTER PROCEDURE dbo.admAdd
    @nome VARCHAR(150), @cpf VARCHAR(14), @email VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    INSERT INTO dbo.Pessoas(nome) VALUES (@nome);
    DECLARE @pessoa_id INT = CONVERT(INT, SCOPE_IDENTITY());
    INSERT INTO dbo.Administradores(pessoa_id,cpf,email) VALUES (@pessoa_id,@cpf,@email);
    COMMIT TRANSACTION;
    SELECT @pessoa_id AS pessoa_id;
END;
GO
CREATE OR ALTER PROCEDURE dbo.admUpdate
    @pessoa_id INT, @nome VARCHAR(150), @cpf VARCHAR(14), @email VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Pessoas SET nome=@nome WHERE id=@pessoa_id;
    UPDATE dbo.Administradores SET cpf=@cpf,email=@email WHERE pessoa_id=@pessoa_id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.funcAdd
    @nome VARCHAR(150), @ativo BIT = 1
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    INSERT INTO dbo.Pessoas(nome) VALUES (@nome);
    DECLARE @pessoa_id INT = CONVERT(INT, SCOPE_IDENTITY());
    INSERT INTO dbo.Funcionarios(pessoa_id,ativo) VALUES (@pessoa_id,@ativo);
    COMMIT TRANSACTION;
    SELECT @pessoa_id AS pessoa_id;
END;
GO
CREATE OR ALTER PROCEDURE dbo.funcUpdate
    @pessoa_id INT, @nome VARCHAR(150), @ativo BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Pessoas SET nome=@nome WHERE id=@pessoa_id;
    UPDATE dbo.Funcionarios SET ativo=@ativo WHERE pessoa_id=@pessoa_id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.cliAdd
    @nome VARCHAR(150), @cpf VARCHAR(14)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    INSERT INTO dbo.Pessoas(nome) VALUES (@nome);
    DECLARE @pessoa_id INT = CONVERT(INT, SCOPE_IDENTITY());
    INSERT INTO dbo.Clientes(pessoa_id,cpf) VALUES (@pessoa_id,@cpf);
    COMMIT TRANSACTION;
    SELECT @pessoa_id AS pessoa_id;
END;
GO
CREATE OR ALTER PROCEDURE dbo.cliUpdate
    @pessoa_id INT, @nome VARCHAR(150), @cpf VARCHAR(14)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Pessoas SET nome=@nome WHERE id=@pessoa_id;
    UPDATE dbo.Clientes SET cpf=@cpf WHERE pessoa_id=@pessoa_id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.fornAdd
    @nome VARCHAR(150), @cnpj VARCHAR(18)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    INSERT INTO dbo.Pessoas(nome) VALUES (@nome);
    DECLARE @pessoa_id INT = CONVERT(INT, SCOPE_IDENTITY());
    INSERT INTO dbo.Fornecedores(pessoa_id,cnpj) VALUES (@pessoa_id,@cnpj);
    COMMIT TRANSACTION;
    SELECT @pessoa_id AS pessoa_id;
END;
GO
CREATE OR ALTER PROCEDURE dbo.fornUpdate
    @pessoa_id INT, @nome VARCHAR(150), @cnpj VARCHAR(18)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Pessoas SET nome=@nome WHERE id=@pessoa_id;
    UPDATE dbo.Fornecedores SET cnpj=@cnpj WHERE pessoa_id=@pessoa_id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.catAdd
    @nome VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Categorias(nome) VALUES (@nome);
    SELECT CONVERT(INT,SCOPE_IDENTITY()) AS categoria_codigo;
END;
GO
CREATE OR ALTER PROCEDURE dbo.catUpdate
    @categoria_codigo INT, @nome VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Categorias SET nome=@nome WHERE codigo=@categoria_codigo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.prodAdd
    @codigo_barras VARCHAR(50), @nome VARCHAR(150), @cor VARCHAR(60),
    @preco_unit DECIMAL(10,2), @estoque INT, @categoria_codigo INT, @ativo BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Produtos(codigo_barras,nome,cor,preco_unit,estoque,categoria_codigo,ativo)
    VALUES (@codigo_barras,@nome,@cor,@preco_unit,@estoque,@categoria_codigo,@ativo);
    SELECT CONVERT(INT,SCOPE_IDENTITY()) AS produto_codigo;
END;
GO
CREATE OR ALTER PROCEDURE dbo.prodUpdate
    @produto_codigo INT, @codigo_barras VARCHAR(50), @nome VARCHAR(150), @cor VARCHAR(60),
    @preco_unit DECIMAL(10,2), @estoque INT, @categoria_codigo INT, @ativo BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Produtos SET codigo_barras=@codigo_barras,nome=@nome,cor=@cor,
        preco_unit=@preco_unit,estoque=@estoque,categoria_codigo=@categoria_codigo,ativo=@ativo
    WHERE codigo=@produto_codigo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.compraEstoqueAdd
    @data_compra DATETIME, @total DECIMAL(10,2), @administrador_id INT,
    @fornecedor_nome VARCHAR(150), @status INT = 1, @fornecedor_id INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.CompraEstoque(data_compra,total,status,administrador_id,fornecedor_id,fornecedor_nome)
    VALUES (@data_compra,@total,@status,@administrador_id,@fornecedor_id,@fornecedor_nome);
    SELECT CONVERT(INT,SCOPE_IDENTITY()) AS compra_numero;
END;
GO
CREATE OR ALTER PROCEDURE dbo.compraEstoqueUpdate
    @numero INT, @data_compra DATETIME, @total DECIMAL(10,2), @status INT,
    @administrador_id INT, @fornecedor_nome VARCHAR(150), @fornecedor_id INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.CompraEstoque SET data_compra=@data_compra,total=@total,status=@status,
        administrador_id=@administrador_id,fornecedor_id=@fornecedor_id,fornecedor_nome=@fornecedor_nome
    WHERE numero=@numero;
END;
GO

CREATE OR ALTER PROCEDURE dbo.pedidoAdd
    @valor_total DECIMAL(10,2), @funcionario_id INT, @cliente_id INT = NULL,
    @status INT = 0, @data_pedido DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Pedidos(status,valor_total,cliente_id,funcionario_id,data_pedido)
    VALUES (@status,@valor_total,@cliente_id,@funcionario_id,COALESCE(@data_pedido,GETDATE()));
    SELECT CONVERT(INT,SCOPE_IDENTITY()) AS pedido_codigo;
END;
GO
CREATE OR ALTER PROCEDURE dbo.pedidoUpdate
    @pedido_codigo INT, @status INT, @valor_total DECIMAL(10,2), @funcionario_id INT,
    @cliente_id INT = NULL, @data_pedido DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Pedidos SET status=@status,valor_total=@valor_total,cliente_id=@cliente_id,
        funcionario_id=@funcionario_id,data_pedido=COALESCE(@data_pedido,data_pedido)
    WHERE codigo=@pedido_codigo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.itemCompraAdd
    @compra_numero INT, @produto_codigo INT, @cor VARCHAR(60), @quantidade INT, @valor DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Itens_CompraEstoque(compra_numero,produto_codigo,cor,quantidade,valor)
    VALUES (@compra_numero,@produto_codigo,@cor,@quantidade,@valor);
END;
GO
CREATE OR ALTER PROCEDURE dbo.itemCompraUpdate
    @compra_numero INT, @produto_codigo INT, @cor VARCHAR(60), @quantidade INT, @valor DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Itens_CompraEstoque SET cor=@cor,quantidade=@quantidade,valor=@valor
    WHERE compra_numero=@compra_numero AND produto_codigo=@produto_codigo;
END;
GO
CREATE OR ALTER PROCEDURE dbo.itemPedidoAdd
    @pedido_codigo INT, @produto_codigo INT, @qtd INT, @preco_total DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Itens_Pedidos(pedido_codigo,produto_codigo,qtd,preco_total)
    VALUES (@pedido_codigo,@produto_codigo,@qtd,@preco_total);
END;
GO
CREATE OR ALTER PROCEDURE dbo.itemPedidoUpdate
    @pedido_codigo INT, @produto_codigo INT, @qtd INT, @preco_total DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Itens_Pedidos SET qtd=@qtd,preco_total=@preco_total
    WHERE pedido_codigo=@pedido_codigo AND produto_codigo=@produto_codigo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.devolucaoAdd
    @produto_codigo INT, @quantidade INT, @valor_total DECIMAL(10,2), @data_devolucao DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Devolucoes(produto_codigo,quantidade,valor_total,data_devolucao)
    VALUES (@produto_codigo,@quantidade,@valor_total,COALESCE(@data_devolucao,GETDATE()));
    SELECT CONVERT(INT,SCOPE_IDENTITY()) AS devolucao_numero;
END;
GO
CREATE OR ALTER PROCEDURE dbo.devolucaoUpdate
    @numero INT, @produto_codigo INT, @quantidade INT, @valor_total DECIMAL(10,2), @data_devolucao DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Devolucoes SET produto_codigo=@produto_codigo,quantidade=@quantidade,
        valor_total=@valor_total,data_devolucao=@data_devolucao WHERE numero=@numero;
END;
GO

CREATE OR ALTER PROCEDURE dbo.credencialAdd
    @pessoa_id INT, @perfil VARCHAR(20), @email VARCHAR(254), @senha_hash VARCHAR(200), @ativo BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.CredenciaisAcesso(pessoa_id,perfil,email,senha_hash,ativo)
    VALUES (@pessoa_id,@perfil,@email,@senha_hash,@ativo);
END;
GO
CREATE OR ALTER PROCEDURE dbo.credencialUpdate
    @pessoa_id INT, @perfil VARCHAR(20), @email VARCHAR(254), @senha_hash VARCHAR(200), @ativo BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.CredenciaisAcesso SET perfil=@perfil,email=@email,senha_hash=@senha_hash,ativo=@ativo
    WHERE pessoa_id=@pessoa_id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.solicitacaoRecuperacaoAdd
    @pessoa_id INT, @status VARCHAR(12) = 'Pendente', @data_solicitacao DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.SolicitacoesRecuperacaoSenha(pessoa_id,data_solicitacao,status)
    VALUES (@pessoa_id,COALESCE(@data_solicitacao,SYSDATETIME()),@status);
    SELECT CONVERT(INT,SCOPE_IDENTITY()) AS solicitacao_id;
END;
GO
CREATE OR ALTER PROCEDURE dbo.solicitacaoRecuperacaoUpdate
    @solicitacao_id INT, @status VARCHAR(12)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.SolicitacoesRecuperacaoSenha SET status=@status
    WHERE solicitacao_id=@solicitacao_id;
END;
GO

SELECT name AS procedure_criada
FROM sys.procedures
WHERE name IN ('admAdd','admUpdate','funcAdd','funcUpdate','cliAdd','cliUpdate','fornAdd','fornUpdate',
'catAdd','catUpdate','prodAdd','prodUpdate','compraEstoqueAdd','compraEstoqueUpdate','pedidoAdd',
'pedidoUpdate','itemCompraAdd','itemCompraUpdate','itemPedidoAdd','itemPedidoUpdate','devolucaoAdd',
'devolucaoUpdate','credencialAdd','credencialUpdate','solicitacaoRecuperacaoAdd','solicitacaoRecuperacaoUpdate');
GO
