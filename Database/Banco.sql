-- 1. TABELA DE ENTIDADE BASE (HERANÇA)
-- ----------------------------------------------------------------------------

IF DB_ID(N'SQLVendas2026') IS NULL
    EXEC(N'CREATE DATABASE SQLVendas2026');
GO

USE SQLVendas2026;
GO


CREATE TABLE Pessoas (
    id          INT IDENTITY(1,1),
    nome        VARCHAR(150)        NOT NULL,

    --Restrições--
    CONSTRAINT PK_Pessoas PRIMARY KEY (id)
); 


-- 2. TABELAS ESPECIALIZADAS (HERANÇA - RELACIONAMENTO ISA)
-- ----------------------------------------------------------------------------

CREATE TABLE Administradores (
    pessoa_id   INT             NOT NULL,
    cpf         VARCHAR(14)     NOT NULL,
    email       VARCHAR(100)    NOT NULL,

    --Restrições--
    CONSTRAINT PK_Administradores PRIMARY KEY (pessoa_id),
    CONSTRAINT UQ_Administradores_CPF UNIQUE (cpf),
    CONSTRAINT FK_Administradores_Pessoas FOREIGN KEY (pessoa_id)
        REFERENCES Pessoas(id) ON DELETE CASCADE
);


CREATE TABLE Fornecedores (
    pessoa_id   INT             NOT NULL,
    cnpj        VARCHAR(18)     NOT NULL,

    --Restrições--
    CONSTRAINT PK_Fornecedores PRIMARY KEY (pessoa_id),
    CONSTRAINT UQ_Fornecedores_CNPJ UNIQUE (cnpj),
    CONSTRAINT FK_Fornecedores_Pessoas FOREIGN KEY (pessoa_id)
        REFERENCES Pessoas(id) ON DELETE CASCADE
);


CREATE TABLE Clientes (
    pessoa_id   INT             NOT NULL,
    cpf         VARCHAR(14)     NOT NULL,

    --Restrições--
    CONSTRAINT PK_Clientes PRIMARY KEY (pessoa_id),
    CONSTRAINT UQ_Clientes_CPF UNIQUE (cpf),
    CONSTRAINT FK_Clientes_Pessoas FOREIGN KEY (pessoa_id)
        REFERENCES Pessoas(id) ON DELETE CASCADE
);


CREATE TABLE Funcionarios (
    pessoa_id   INT             NOT NULL,
    ativo       BIT             NOT NULL CONSTRAINT DF_Funcionarios_Ativo DEFAULT 1,

    --Restrições--
    CONSTRAINT PK_Funcionarios PRIMARY KEY (pessoa_id),
    CONSTRAINT FK_Funcionarios_Pessoas FOREIGN KEY (pessoa_id)
        REFERENCES Pessoas(id) ON DELETE CASCADE
);


-- 3. TABELAS DE APOIO INDEPENDENTES
-- ----------------------------------------------------------------------------

CREATE TABLE Categorias (
    codigo      INT IDENTITY(1,1),
    nome        VARCHAR(100)        NOT NULL,

    --Restrições--
    CONSTRAINT PK_Categorias PRIMARY KEY (codigo)
);


CREATE TABLE Produtos (
    codigo_barras       VARCHAR(50)        NOT NULL,
    codigo              INT IDENTITY(1,1),
    nome                VARCHAR(150)       NOT NULL,
    cor                 VARCHAR(60)        NOT NULL CONSTRAINT DF_Produtos_Cor DEFAULT ('Não informada'),
    preco_unit          DECIMAL(10,2)      NOT NULL,
    estoque             INT                NOT NULL
        CONSTRAINT DF_Produtos_Estoque DEFAULT 0,
    categoria_codigo    INT                NOT NULL,
    ativo               BIT                NOT NULL CONSTRAINT DF_Produtos_Ativo DEFAULT 1,

    --Restrições--
    CONSTRAINT PK_Produtos PRIMARY KEY (codigo),
    CONSTRAINT UQ_Produtos_CodigoBarras UNIQUE (codigo_barras),

    CONSTRAINT FK_Produtos_Categorias
        FOREIGN KEY (categoria_codigo)
        REFERENCES Categorias(codigo)
);


-- 4. TABELAS DE OPERAÇÕES E MOVIMENTAÇÃO (ENTIDADES FORTES)
-- ----------------------------------------------------------------------------

CREATE TABLE CompraEstoque (
    numero              INT IDENTITY(1,1),
    data_compra         DATETIME           NOT NULL
        CONSTRAINT DF_CompraEstoque_Data DEFAULT GETDATE(),
    total               DECIMAL(10,2)      NOT NULL,
    status              INT                NOT NULL,
    administrador_id    INT                NOT NULL,
    fornecedor_id       INT                NULL,
    fornecedor_nome     VARCHAR(150)       NOT NULL,

    --Restrições--
    CONSTRAINT PK_CompraEstoque PRIMARY KEY (numero),

    CONSTRAINT FK_CompraEstoque_Administradores
        FOREIGN KEY (administrador_id)
        REFERENCES Administradores(pessoa_id),

    CONSTRAINT FK_CompraEstoque_Fornecedores
        FOREIGN KEY (fornecedor_id)
        REFERENCES Fornecedores(pessoa_id)
);


CREATE TABLE Pedidos (
    codigo              INT IDENTITY(1,1),
    status              INT                NOT NULL,
    valor_total         DECIMAL(10,2)      NOT NULL,
    data_pedido         DATETIME2          NOT NULL
        CONSTRAINT DF_Pedidos_Data DEFAULT GETDATE(),
    cliente_id          INT                NULL,
    funcionario_id      INT                NOT NULL,

    --Restrições--
    CONSTRAINT PK_Pedidos PRIMARY KEY (codigo),

    CONSTRAINT FK_Pedidos_Clientes
        FOREIGN KEY (cliente_id)
        REFERENCES Clientes(pessoa_id),

    CONSTRAINT FK_Pedidos_Funcionarios
        FOREIGN KEY (funcionario_id)
        REFERENCES Funcionarios(pessoa_id)
);


-- 5. TABELAS ASSOCIATIVAS (RELACIONAMENTOS N:N)
-- ----------------------------------------------------------------------------

-- Mapeamento da relação "Tem" entre CompraEstoque e Produtos

CREATE TABLE Itens_CompraEstoque (
    compra_numero       INT             NOT NULL,
    produto_codigo      INT             NOT NULL,
    cor                 VARCHAR(60)     NOT NULL CONSTRAINT DF_Itens_CompraEstoque_Cor DEFAULT ('Não informada'),
    quantidade          INT             NOT NULL CHECK (quantidade > 0),
    valor               DECIMAL(10,2)   NOT NULL,

    --Restrições--
    CONSTRAINT PK_Itens_CompraEstoque
        PRIMARY KEY (compra_numero, produto_codigo),

    CONSTRAINT FK_Itens_CompraEstoque_CompraEstoque
        FOREIGN KEY (compra_numero)
        REFERENCES CompraEstoque(numero) ON DELETE CASCADE,

    CONSTRAINT FK_Itens_CompraEstoque_Produtos
        FOREIGN KEY (produto_codigo)
        REFERENCES Produtos(codigo)
);


-- Mapeamento da relação "Tem" entre Pedidos e Produtos

CREATE TABLE Itens_Pedidos (
    pedido_codigo       INT             NOT NULL,
    produto_codigo      INT             NOT NULL,
    qtd                 INT             NOT NULL CHECK (qtd > 0),
    preco_total         DECIMAL(10,2)   NOT NULL,

    --Restrições--
    CONSTRAINT PK_Itens_Pedidos
        PRIMARY KEY (pedido_codigo, produto_codigo),

    CONSTRAINT FK_Itens_Pedidos_Pedidos
        FOREIGN KEY (pedido_codigo)
        REFERENCES Pedidos(codigo) ON DELETE CASCADE,

    CONSTRAINT FK_Itens_Pedidos_Produtos
        FOREIGN KEY (produto_codigo)
        REFERENCES Produtos(codigo)
);

CREATE TABLE Devolucoes (
    numero              INT IDENTITY(1,1),
    data_devolucao      DATETIME2         NOT NULL CONSTRAINT DF_Devolucoes_Data DEFAULT SYSDATETIME(),
    produto_codigo      INT               NOT NULL,
    quantidade          INT               NOT NULL CHECK (quantidade > 0),
    valor_total         DECIMAL(10,2)     NOT NULL CHECK (valor_total >= 0),

    CONSTRAINT PK_Devolucoes PRIMARY KEY (numero),
    CONSTRAINT FK_Devolucoes_Produtos FOREIGN KEY (produto_codigo)
        REFERENCES Produtos(codigo)
);


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
