-- 1. TABELA DE ENTIDADE BASE (HERANÇA)
-- ----------------------------------------------------------------------------

CREATE DATABASE SQLVendas2026;

USE SQLVendas2026;


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
    preco_unit          DECIMAL(10,2)      NOT NULL,
    estoque             INT                NOT NULL
        CONSTRAINT DF_Produtos_Estoque DEFAULT 0,
    categoria_codigo    INT                NOT NULL,

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
    fornecedor_id       INT                NOT NULL,

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
    cliente_id          INT                NOT NULL,
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


-- ============================================================================
-- INSERTS DAS TABELAS
-- ============================================================================


-- ----------------------------------------------------------------------------
-- 1. PESSOAS
-- ----------------------------------------------------------------------------

INSERT INTO Pessoas (nome)
VALUES
('Carlos Silva'),
('Mariana Souza'),
('Fornecedor Alfa'),
('Joao Pereira'),
('Fernanda Lima');


-- ----------------------------------------------------------------------------
-- 2. ADMINISTRADORES
-- ----------------------------------------------------------------------------

INSERT INTO Administradores (pessoa_id, cpf, email)
VALUES
(1, '111.111.111-11', 'carlos@empresa.com');


-- ----------------------------------------------------------------------------
-- 3. FORNECEDORES
-- ----------------------------------------------------------------------------

INSERT INTO Fornecedores (pessoa_id, cnpj)
VALUES
(3, '12.345.678/0001-99');


-- ----------------------------------------------------------------------------
-- 4. CLIENTES
-- ----------------------------------------------------------------------------

INSERT INTO Clientes (pessoa_id, cpf)
VALUES
(2, '222.222.222-22');


-- ----------------------------------------------------------------------------
-- 5. FUNCIONARIOS
-- ----------------------------------------------------------------------------

INSERT INTO Funcionarios (pessoa_id)
VALUES
(4),
(5);


-- ----------------------------------------------------------------------------
-- 6. CATEGORIAS
-- ----------------------------------------------------------------------------

INSERT INTO Categorias (nome)
VALUES
('Masculino'),
('Feminino'),
('Acessorios');


-- ----------------------------------------------------------------------------
-- 7. PRODUTOS
-- ----------------------------------------------------------------------------

INSERT INTO Produtos
(codigo_barras, nome, preco_unit, estoque, categoria_codigo)
VALUES
('7891234567890', 'Camiseta Basica Masculina', 59.90, 50, 1),
('7891234567891', 'Calca Jeans Masculina', 149.90, 30, 1),
('7891234567892', 'Vestido Floral Feminino', 189.90, 20, 2),
('7891234567893', 'Bolsa Feminina', 129.90, 15, 3);


-- ----------------------------------------------------------------------------
-- 8. COMPRA_ESTOQUE
-- ----------------------------------------------------------------------------

INSERT INTO CompraEstoque
(total, status, administrador_id, fornecedor_id)
VALUES
(2697.00, 1, 1, 3);


-- ----------------------------------------------------------------------------
-- 9. PEDIDOS
-- ----------------------------------------------------------------------------

INSERT INTO Pedidos
(status, valor_total, cliente_id, funcionario_id)
VALUES
(1, 359.70, 2, 4),
(1, 189.90, 2, 5);


-- ----------------------------------------------------------------------------
-- 10. ITENS_COMPRA_ESTOQUE
-- ----------------------------------------------------------------------------

INSERT INTO Itens_CompraEstoque
(compra_numero, produto_codigo, quantidade, valor)
VALUES
(1, 1, 20, 1198.00),
(1, 2, 10, 1499.00);


-- ----------------------------------------------------------------------------
-- 11. ITENS_PEDIDOS
-- ----------------------------------------------------------------------------

INSERT INTO Itens_Pedidos
(pedido_codigo, produto_codigo, qtd, preco_total)
VALUES
(1, 1, 1, 59.90),
(1, 2, 2, 299.80),
(2, 3, 1, 189.90);


select * from Pessoas;