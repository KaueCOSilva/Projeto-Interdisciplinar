-- O catálogo precisa ser igual ao Initial Catalog da connection string da aplicação.
USE [SQLVendas2026];
GO

SELECT DB_NAME() AS banco_alvo;
GO

IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Produtos', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Administradores', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Categorias', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Funcionarios', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Itens_CompraEstoque', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Itens_Pedidos', N'U') IS NULL
   OR OBJECT_ID(N'dbo.CompraEstoque', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Pessoas', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Fornecedores', N'U') IS NULL
BEGIN
    ;
    THROW 51000, 'O banco selecionado nao contem o schema SIVAD esperado. Selecione o mesmo banco configurado no appsettings.json antes de executar a migracao.', 1;
END;
GO

-- Dados necessários aos gráficos cronológicos em bases antigas.
-- Pedidos podem ser registrados sem associação a cliente.
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Pedidos')
      AND name = N'cliente_id'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.Pedidos ALTER COLUMN cliente_id INT NULL;
END;
GO

IF COL_LENGTH('dbo.Pedidos', 'data_pedido') IS NULL
BEGIN
    ALTER TABLE dbo.Pedidos
    ADD data_pedido DATETIME2 NOT NULL
        CONSTRAINT DF_Pedidos_Data DEFAULT (GETDATE());
END;
GO

-- Remoção lógica preserva referências históricas.
IF COL_LENGTH('dbo.Produtos', 'ativo') IS NULL
BEGIN
    ALTER TABLE dbo.Produtos
    ADD ativo BIT NOT NULL
        CONSTRAINT DF_Produtos_Ativo DEFAULT (1);
END;
GO

IF COL_LENGTH('dbo.Funcionarios', 'ativo') IS NULL
BEGIN
    ALTER TABLE dbo.Funcionarios
    ADD ativo BIT NOT NULL
        CONSTRAINT DF_Funcionarios_Ativo DEFAULT (1);
END;
GO

-- Cor atual do produto e cor registrada no item recebido.
-- A coluna e criada NOT NULL com DEFAULT, o que preenche as linhas existentes
-- e evita referenciar a coluna nova no mesmo lote em que ela e criada.
IF COL_LENGTH('dbo.Produtos', 'cor') IS NULL
    ALTER TABLE dbo.Produtos
    ADD cor VARCHAR(60) NOT NULL
        CONSTRAINT DF_Produtos_Cor DEFAULT ('Não informada');
GO

IF COL_LENGTH('dbo.Itens_CompraEstoque', 'cor') IS NULL
    ALTER TABLE dbo.Itens_CompraEstoque
    ADD cor VARCHAR(60) NOT NULL
        CONSTRAINT DF_Itens_CompraEstoque_Cor DEFAULT ('Não informada');
GO

-- Guarda o nome digitado em cada compra, sem exigir seleção/cadastro de fornecedor.
IF COL_LENGTH('dbo.CompraEstoque', 'fornecedor_nome') IS NULL
BEGIN
    ALTER TABLE dbo.CompraEstoque ADD fornecedor_nome VARCHAR(150) NULL;
END;
GO

UPDATE ce
SET fornecedor_nome = COALESCE(NULLIF(LTRIM(RTRIM(p.nome)), ''), 'Fornecedor não informado')
FROM dbo.CompraEstoque ce
LEFT JOIN dbo.Fornecedores f ON f.pessoa_id = ce.fornecedor_id
LEFT JOIN dbo.Pessoas p ON p.id = f.pessoa_id
WHERE ce.fornecedor_nome IS NULL OR LTRIM(RTRIM(ce.fornecedor_nome)) = '';
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CompraEstoque')
      AND name = N'fornecedor_id'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.CompraEstoque ALTER COLUMN fornecedor_id INT NULL;
END;
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CompraEstoque')
      AND name = N'fornecedor_nome'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.CompraEstoque ALTER COLUMN fornecedor_nome VARCHAR(150) NOT NULL;
END;
GO

-- Registros fictícios para demonstração. Não são contas de login.
IF NOT EXISTS (
    SELECT 1
    FROM dbo.Administradores
    WHERE cpf = '000.000.000-00'
)
BEGIN
    INSERT INTO dbo.Pessoas (nome)
    VALUES ('Administrador de Demonstração');

    DECLARE @AdministradorDemoId INT = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.Administradores (pessoa_id, cpf, email)
    VALUES (@AdministradorDemoId, '000.000.000-00', 'admin.demo@example.invalid');
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.Funcionarios f
    INNER JOIN dbo.Pessoas p ON p.id = f.pessoa_id
    WHERE p.nome = 'Funcionário de Demonstração'
      AND f.ativo = 1
)
BEGIN
    INSERT INTO dbo.Pessoas (nome)
    VALUES ('Funcionário de Demonstração');

    DECLARE @FuncionarioDemoId INT = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.Funcionarios (pessoa_id)
    VALUES (@FuncionarioDemoId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.Categorias
    WHERE nome = N'Vestuário'
)
BEGIN
    INSERT INTO dbo.Categorias (nome)
    VALUES (N'Vestuário');
END;
GO

IF OBJECT_ID(N'dbo.Devolucoes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Devolucoes (
        numero INT IDENTITY(1,1) NOT NULL,
        data_devolucao DATETIME2 NOT NULL
            CONSTRAINT DF_Devolucoes_Data DEFAULT SYSDATETIME(),
        produto_codigo INT NOT NULL,
        quantidade INT NOT NULL CHECK (quantidade > 0),
        valor_total DECIMAL(10,2) NOT NULL CHECK (valor_total >= 0),
        CONSTRAINT PK_Devolucoes PRIMARY KEY (numero),
        CONSTRAINT FK_Devolucoes_Produtos FOREIGN KEY (produto_codigo)
            REFERENCES dbo.Produtos(codigo)
    );
END;
GO

-- Todos os campos abaixo devem retornar um valor diferente de NULL.
SELECT DB_NAME() AS banco_migrado,
       COL_LENGTH('dbo.Pedidos', 'data_pedido') AS data_pedido,
       COL_LENGTH('dbo.Produtos', 'ativo') AS produto_ativo,
       COL_LENGTH('dbo.Funcionarios', 'ativo') AS funcionario_ativo,
       COL_LENGTH('dbo.CompraEstoque', 'fornecedor_nome') AS fornecedor_nome,
       COL_LENGTH('dbo.Produtos', 'cor') AS produto_cor,
       COL_LENGTH('dbo.Itens_CompraEstoque', 'cor') AS item_compra_cor,
       COL_LENGTH('dbo.Devolucoes', 'numero') AS devolucoes;
