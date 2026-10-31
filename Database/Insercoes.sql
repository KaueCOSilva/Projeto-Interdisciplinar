/*
    SIVAD — inserts demonstrativos do documento
    Execute depois de Banco.sql/AtualizarBanco.sql e antes de Views.sql.
    Os IF NOT EXISTS permitem executar este arquivo novamente sem duplicar a carga.
*/
USE SQLVendas2026;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @idAna INT, @idBruno INT, @idCarolina INT;
DECLARE @idFornecedorAlfa INT, @idFornecedorBeta INT, @idFornecedorGama INT;
DECLARE @idClienteUm INT, @idClienteDois INT, @idClienteTres INT;
DECLARE @idJoao INT, @idMaria INT, @idPedro INT;

IF NOT EXISTS (SELECT 1 FROM dbo.Pessoas WHERE nome = 'Ana Administradora') INSERT INTO dbo.Pessoas(nome) VALUES ('Ana Administradora');
SELECT @idAna = id FROM dbo.Pessoas WHERE nome = 'Ana Administradora';
IF NOT EXISTS (SELECT 1 FROM dbo.Pessoas WHERE nome = 'Bruno Administrador') INSERT INTO dbo.Pessoas(nome) VALUES ('Bruno Administrador');
SELECT @idBruno = id FROM dbo.Pessoas WHERE nome = 'Bruno Administrador';
IF NOT EXISTS (SELECT 1 FROM dbo.Pessoas WHERE nome = 'Carolina Administradora') INSERT INTO dbo.Pessoas(nome) VALUES ('Carolina Administradora');
SELECT @idCarolina = id FROM dbo.Pessoas WHERE nome = 'Carolina Administradora';
IF NOT EXISTS (SELECT 1 FROM dbo.Pessoas WHERE nome = 'Fornecedor Alfa') INSERT INTO dbo.Pessoas(nome) VALUES ('Fornecedor Alfa');
SELECT @idFornecedorAlfa = id FROM dbo.Pessoas WHERE nome = 'Fornecedor Alfa';
IF NOT EXISTS (SELECT 1 FROM dbo.Pessoas WHERE nome = 'Fornecedor Beta') INSERT INTO dbo.Pessoas(nome) VALUES ('Fornecedor Beta');
SELECT @idFornecedorBeta = id FROM dbo.Pessoas WHERE nome = 'Fornecedor Beta';
IF NOT EXISTS (SELECT 1 FROM dbo.Pessoas WHERE nome = 'Fornecedor Gama') INSERT INTO dbo.Pessoas(nome) VALUES ('Fornecedor Gama');
SELECT @idFornecedorGama = id FROM dbo.Pessoas WHERE nome = 'Fornecedor Gama';
IF NOT EXISTS (SELECT 1 FROM dbo.Pessoas WHERE nome = 'Cliente Um') INSERT INTO dbo.Pessoas(nome) VALUES ('Cliente Um');
SELECT @idClienteUm = id FROM dbo.Pessoas WHERE nome = 'Cliente Um';
IF NOT EXISTS (SELECT 1 FROM dbo.Pessoas WHERE nome = 'Cliente Dois') INSERT INTO dbo.Pessoas(nome) VALUES ('Cliente Dois');
SELECT @idClienteDois = id FROM dbo.Pessoas WHERE nome = 'Cliente Dois';
IF NOT EXISTS (SELECT 1 FROM dbo.Pessoas WHERE nome = 'Cliente Tres') INSERT INTO dbo.Pessoas(nome) VALUES ('Cliente Tres');
SELECT @idClienteTres = id FROM dbo.Pessoas WHERE nome = 'Cliente Tres';
IF NOT EXISTS (SELECT 1 FROM dbo.Pessoas WHERE nome = 'Joao Funcionario') INSERT INTO dbo.Pessoas(nome) VALUES ('Joao Funcionario');
SELECT @idJoao = id FROM dbo.Pessoas WHERE nome = 'Joao Funcionario';
IF NOT EXISTS (SELECT 1 FROM dbo.Pessoas WHERE nome = 'Maria Funcionaria') INSERT INTO dbo.Pessoas(nome) VALUES ('Maria Funcionaria');
SELECT @idMaria = id FROM dbo.Pessoas WHERE nome = 'Maria Funcionaria';
IF NOT EXISTS (SELECT 1 FROM dbo.Pessoas WHERE nome = 'Pedro Funcionario') INSERT INTO dbo.Pessoas(nome) VALUES ('Pedro Funcionario');
SELECT @idPedro = id FROM dbo.Pessoas WHERE nome = 'Pedro Funcionario';

IF NOT EXISTS (SELECT 1 FROM dbo.Administradores WHERE pessoa_id = @idAna) INSERT INTO dbo.Administradores(pessoa_id,cpf,email) VALUES (@idAna,'111.111.111-11','admin1@example.invalid');
IF NOT EXISTS (SELECT 1 FROM dbo.Administradores WHERE pessoa_id = @idBruno) INSERT INTO dbo.Administradores(pessoa_id,cpf,email) VALUES (@idBruno,'222.222.222-22','admin2@example.invalid');
IF NOT EXISTS (SELECT 1 FROM dbo.Administradores WHERE pessoa_id = @idCarolina) INSERT INTO dbo.Administradores(pessoa_id,cpf,email) VALUES (@idCarolina,'333.333.333-33','admin3@example.invalid');
IF NOT EXISTS (SELECT 1 FROM dbo.Fornecedores WHERE pessoa_id = @idFornecedorAlfa) INSERT INTO dbo.Fornecedores(pessoa_id,cnpj) VALUES (@idFornecedorAlfa,'11.111.111/0001-11');
IF NOT EXISTS (SELECT 1 FROM dbo.Fornecedores WHERE pessoa_id = @idFornecedorBeta) INSERT INTO dbo.Fornecedores(pessoa_id,cnpj) VALUES (@idFornecedorBeta,'22.222.222/0001-22');
IF NOT EXISTS (SELECT 1 FROM dbo.Fornecedores WHERE pessoa_id = @idFornecedorGama) INSERT INTO dbo.Fornecedores(pessoa_id,cnpj) VALUES (@idFornecedorGama,'33.333.333/0001-33');
IF NOT EXISTS (SELECT 1 FROM dbo.Clientes WHERE pessoa_id = @idClienteUm) INSERT INTO dbo.Clientes(pessoa_id,cpf) VALUES (@idClienteUm,'444.444.444-44');
IF NOT EXISTS (SELECT 1 FROM dbo.Clientes WHERE pessoa_id = @idClienteDois) INSERT INTO dbo.Clientes(pessoa_id,cpf) VALUES (@idClienteDois,'555.555.555-55');
IF NOT EXISTS (SELECT 1 FROM dbo.Clientes WHERE pessoa_id = @idClienteTres) INSERT INTO dbo.Clientes(pessoa_id,cpf) VALUES (@idClienteTres,'666.666.666-66');
IF NOT EXISTS (SELECT 1 FROM dbo.Funcionarios WHERE pessoa_id = @idJoao) INSERT INTO dbo.Funcionarios(pessoa_id,ativo) VALUES (@idJoao,1);
IF NOT EXISTS (SELECT 1 FROM dbo.Funcionarios WHERE pessoa_id = @idMaria) INSERT INTO dbo.Funcionarios(pessoa_id,ativo) VALUES (@idMaria,1);
IF NOT EXISTS (SELECT 1 FROM dbo.Funcionarios WHERE pessoa_id = @idPedro) INSERT INTO dbo.Funcionarios(pessoa_id,ativo) VALUES (@idPedro,1);

DECLARE @catAcessorios INT, @catVestuario INT, @catCalcados INT;
IF NOT EXISTS (SELECT 1 FROM dbo.Categorias WHERE nome = 'Acessorios') INSERT INTO dbo.Categorias(nome) VALUES ('Acessorios');
SELECT @catAcessorios = codigo FROM dbo.Categorias WHERE nome = 'Acessorios';
IF NOT EXISTS (SELECT 1 FROM dbo.Categorias WHERE nome = 'Vestuario') INSERT INTO dbo.Categorias(nome) VALUES ('Vestuario');
SELECT @catVestuario = codigo FROM dbo.Categorias WHERE nome = 'Vestuario';
IF NOT EXISTS (SELECT 1 FROM dbo.Categorias WHERE nome = 'Calcados') INSERT INTO dbo.Categorias(nome) VALUES ('Calcados');
SELECT @catCalcados = codigo FROM dbo.Categorias WHERE nome = 'Calcados';

IF NOT EXISTS (SELECT 1 FROM dbo.Produtos WHERE codigo_barras = '7890000000001')
    INSERT INTO dbo.Produtos(codigo_barras,nome,cor,preco_unit,estoque,categoria_codigo,ativo) VALUES ('7890000000001','Bolsa','Preto',129.90,12,@catAcessorios,1);
IF NOT EXISTS (SELECT 1 FROM dbo.Produtos WHERE codigo_barras = '7890000000002')
    INSERT INTO dbo.Produtos(codigo_barras,nome,cor,preco_unit,estoque,categoria_codigo,ativo) VALUES ('7890000000002','Camiseta','Azul',89.90,13,@catVestuario,1);
IF NOT EXISTS (SELECT 1 FROM dbo.Produtos WHERE codigo_barras = '7890000000003')
    INSERT INTO dbo.Produtos(codigo_barras,nome,cor,preco_unit,estoque,categoria_codigo,ativo) VALUES ('7890000000003','Tenis','Branco',59.90,15,@catCalcados,1);

-- Senhas da apresentação: Sivad@2026! para administradores e
-- Funcionario@2026! para funcionários. O banco recebe somente os hashes.
DECLARE @hashAdmin VARCHAR(200) = 'PBKDF2-SHA256$210000$xrUURskNBAJT7euUhXCarg==$PQk+Fa57IeKcWNJ3N7YsL5wWnkPjlvCgfl8pKmGGI5A=';
DECLARE @hashFuncionario VARCHAR(200) = 'PBKDF2-SHA256$210000$ZfAuUlGhS2cuQwUrUH8cXw==$JzHWTBl8GA4i0ebvv5IclQ0rnRaT85FCeKplqjRD05c=';
IF NOT EXISTS (SELECT 1 FROM dbo.CredenciaisAcesso WHERE pessoa_id = @idAna) INSERT INTO dbo.CredenciaisAcesso(pessoa_id,perfil,email,senha_hash,ativo) VALUES (@idAna,'Administrador','admin1@example.invalid',@hashAdmin,1);
IF NOT EXISTS (SELECT 1 FROM dbo.CredenciaisAcesso WHERE pessoa_id = @idBruno) INSERT INTO dbo.CredenciaisAcesso(pessoa_id,perfil,email,senha_hash,ativo) VALUES (@idBruno,'Administrador','admin2@example.invalid',@hashAdmin,1);
IF NOT EXISTS (SELECT 1 FROM dbo.CredenciaisAcesso WHERE pessoa_id = @idCarolina) INSERT INTO dbo.CredenciaisAcesso(pessoa_id,perfil,email,senha_hash,ativo) VALUES (@idCarolina,'Administrador','admin3@example.invalid',@hashAdmin,1);
IF NOT EXISTS (SELECT 1 FROM dbo.CredenciaisAcesso WHERE pessoa_id = @idJoao) INSERT INTO dbo.CredenciaisAcesso(pessoa_id,perfil,email,senha_hash,ativo) VALUES (@idJoao,'Funcionario','funcionario1@example.invalid',@hashFuncionario,1);
IF NOT EXISTS (SELECT 1 FROM dbo.CredenciaisAcesso WHERE pessoa_id = @idMaria) INSERT INTO dbo.CredenciaisAcesso(pessoa_id,perfil,email,senha_hash,ativo) VALUES (@idMaria,'Funcionario','funcionario2@example.invalid',@hashFuncionario,1);
IF NOT EXISTS (SELECT 1 FROM dbo.CredenciaisAcesso WHERE pessoa_id = @idPedro) INSERT INTO dbo.CredenciaisAcesso(pessoa_id,perfil,email,senha_hash,ativo) VALUES (@idPedro,'Funcionario','funcionario3@example.invalid',@hashFuncionario,1);

DECLARE @produto1 INT, @produto2 INT, @produto3 INT;
SELECT @produto1 = codigo FROM dbo.Produtos WHERE codigo_barras = '7890000000001';
SELECT @produto2 = codigo FROM dbo.Produtos WHERE codigo_barras = '7890000000002';
SELECT @produto3 = codigo FROM dbo.Produtos WHERE codigo_barras = '7890000000003';
DECLARE @compra1 INT, @compra2 INT, @compra3 INT;
IF NOT EXISTS (SELECT 1 FROM dbo.CompraEstoque WHERE data_compra = '2026-10-20T09:00:00' AND administrador_id = @idAna)
    INSERT INTO dbo.CompraEstoque(data_compra,total,status,administrador_id,fornecedor_id,fornecedor_nome) VALUES ('2026-10-20T09:00:00',240.00,1,@idAna,@idFornecedorAlfa,'Fornecedor Alfa');
SELECT @compra1 = numero FROM dbo.CompraEstoque WHERE data_compra = '2026-10-20T09:00:00' AND administrador_id = @idAna;
IF NOT EXISTS (SELECT 1 FROM dbo.CompraEstoque WHERE data_compra = '2026-10-21T10:30:00' AND administrador_id = @idBruno)
    INSERT INTO dbo.CompraEstoque(data_compra,total,status,administrador_id,fornecedor_id,fornecedor_nome) VALUES ('2026-10-21T10:30:00',140.00,1,@idBruno,@idFornecedorBeta,'Fornecedor Beta');
SELECT @compra2 = numero FROM dbo.CompraEstoque WHERE data_compra = '2026-10-21T10:30:00' AND administrador_id = @idBruno;
IF NOT EXISTS (SELECT 1 FROM dbo.CompraEstoque WHERE data_compra = '2026-10-22T14:15:00' AND administrador_id = @idCarolina)
    INSERT INTO dbo.CompraEstoque(data_compra,total,status,administrador_id,fornecedor_id,fornecedor_nome) VALUES ('2026-10-22T14:15:00',110.00,1,@idCarolina,@idFornecedorGama,'Fornecedor Gama');
SELECT @compra3 = numero FROM dbo.CompraEstoque WHERE data_compra = '2026-10-22T14:15:00' AND administrador_id = @idCarolina;
IF NOT EXISTS (SELECT 1 FROM dbo.Itens_CompraEstoque WHERE compra_numero=@compra1 AND produto_codigo=@produto1) INSERT INTO dbo.Itens_CompraEstoque(compra_numero,produto_codigo,cor,quantidade,valor) VALUES (@compra1,@produto1,'Preto',2,120.00);
IF NOT EXISTS (SELECT 1 FROM dbo.Itens_CompraEstoque WHERE compra_numero=@compra1 AND produto_codigo=@produto2) INSERT INTO dbo.Itens_CompraEstoque(compra_numero,produto_codigo,cor,quantidade,valor) VALUES (@compra1,@produto2,'Azul',3,120.00);
IF NOT EXISTS (SELECT 1 FROM dbo.Itens_CompraEstoque WHERE compra_numero=@compra2 AND produto_codigo=@produto2) INSERT INTO dbo.Itens_CompraEstoque(compra_numero,produto_codigo,cor,quantidade,valor) VALUES (@compra2,@produto2,'Azul',1,40.00);
IF NOT EXISTS (SELECT 1 FROM dbo.Itens_CompraEstoque WHERE compra_numero=@compra2 AND produto_codigo=@produto3) INSERT INTO dbo.Itens_CompraEstoque(compra_numero,produto_codigo,cor,quantidade,valor) VALUES (@compra2,@produto3,'Branco',4,100.00);
IF NOT EXISTS (SELECT 1 FROM dbo.Itens_CompraEstoque WHERE compra_numero=@compra3 AND produto_codigo=@produto1) INSERT INTO dbo.Itens_CompraEstoque(compra_numero,produto_codigo,cor,quantidade,valor) VALUES (@compra3,@produto1,'Preto',1,60.00);
IF NOT EXISTS (SELECT 1 FROM dbo.Itens_CompraEstoque WHERE compra_numero=@compra3 AND produto_codigo=@produto3) INSERT INTO dbo.Itens_CompraEstoque(compra_numero,produto_codigo,cor,quantidade,valor) VALUES (@compra3,@produto3,'Branco',2,50.00);

DECLARE @pedido1 INT, @pedido2 INT, @pedido3 INT;
IF NOT EXISTS (SELECT 1 FROM dbo.Pedidos WHERE data_pedido='2026-10-25T12:00:00' AND funcionario_id=@idJoao) INSERT INTO dbo.Pedidos(status,valor_total,data_pedido,cliente_id,funcionario_id) VALUES (1,219.80,'2026-10-25T12:00:00',@idClienteUm,@idJoao);
SELECT @pedido1=codigo FROM dbo.Pedidos WHERE data_pedido='2026-10-25T12:00:00' AND funcionario_id=@idJoao;
IF NOT EXISTS (SELECT 1 FROM dbo.Pedidos WHERE data_pedido='2026-10-26T13:30:00' AND funcionario_id=@idMaria) INSERT INTO dbo.Pedidos(status,valor_total,data_pedido,cliente_id,funcionario_id) VALUES (1,149.80,'2026-10-26T13:30:00',@idClienteDois,@idMaria);
SELECT @pedido2=codigo FROM dbo.Pedidos WHERE data_pedido='2026-10-26T13:30:00' AND funcionario_id=@idMaria;
IF NOT EXISTS (SELECT 1 FROM dbo.Pedidos WHERE data_pedido='2026-10-27T16:45:00' AND funcionario_id=@idPedro) INSERT INTO dbo.Pedidos(status,valor_total,data_pedido,cliente_id,funcionario_id) VALUES (1,189.80,'2026-10-27T16:45:00',@idClienteTres,@idPedro);
SELECT @pedido3=codigo FROM dbo.Pedidos WHERE data_pedido='2026-10-27T16:45:00' AND funcionario_id=@idPedro;
IF NOT EXISTS (SELECT 1 FROM dbo.Itens_Pedidos WHERE pedido_codigo=@pedido1 AND produto_codigo=@produto1) INSERT INTO dbo.Itens_Pedidos(pedido_codigo,produto_codigo,qtd,preco_total) VALUES (@pedido1,@produto1,1,129.90);
IF NOT EXISTS (SELECT 1 FROM dbo.Itens_Pedidos WHERE pedido_codigo=@pedido1 AND produto_codigo=@produto2) INSERT INTO dbo.Itens_Pedidos(pedido_codigo,produto_codigo,qtd,preco_total) VALUES (@pedido1,@produto2,1,89.90);
IF NOT EXISTS (SELECT 1 FROM dbo.Itens_Pedidos WHERE pedido_codigo=@pedido2 AND produto_codigo=@produto2) INSERT INTO dbo.Itens_Pedidos(pedido_codigo,produto_codigo,qtd,preco_total) VALUES (@pedido2,@produto2,1,89.90);
IF NOT EXISTS (SELECT 1 FROM dbo.Itens_Pedidos WHERE pedido_codigo=@pedido2 AND produto_codigo=@produto3) INSERT INTO dbo.Itens_Pedidos(pedido_codigo,produto_codigo,qtd,preco_total) VALUES (@pedido2,@produto3,1,59.90);
IF NOT EXISTS (SELECT 1 FROM dbo.Itens_Pedidos WHERE pedido_codigo=@pedido3 AND produto_codigo=@produto1) INSERT INTO dbo.Itens_Pedidos(pedido_codigo,produto_codigo,qtd,preco_total) VALUES (@pedido3,@produto1,1,129.90);
IF NOT EXISTS (SELECT 1 FROM dbo.Itens_Pedidos WHERE pedido_codigo=@pedido3 AND produto_codigo=@produto3) INSERT INTO dbo.Itens_Pedidos(pedido_codigo,produto_codigo,qtd,preco_total) VALUES (@pedido3,@produto3,1,59.90);

IF NOT EXISTS (SELECT 1 FROM dbo.Devolucoes WHERE produto_codigo=@produto1 AND data_devolucao='2026-10-28T09:00:00') INSERT INTO dbo.Devolucoes(data_devolucao,produto_codigo,quantidade,valor_total) VALUES ('2026-10-28T09:00:00',@produto1,1,129.90);
IF NOT EXISTS (SELECT 1 FROM dbo.Devolucoes WHERE produto_codigo=@produto2 AND data_devolucao='2026-10-28T10:00:00') INSERT INTO dbo.Devolucoes(data_devolucao,produto_codigo,quantidade,valor_total) VALUES ('2026-10-28T10:00:00',@produto2,1,89.90);
IF NOT EXISTS (SELECT 1 FROM dbo.Devolucoes WHERE produto_codigo=@produto3 AND data_devolucao='2026-10-28T11:00:00') INSERT INTO dbo.Devolucoes(data_devolucao,produto_codigo,quantidade,valor_total) VALUES ('2026-10-28T11:00:00',@produto3,1,59.90);
IF NOT EXISTS (SELECT 1 FROM dbo.SolicitacoesRecuperacaoSenha WHERE pessoa_id=@idJoao AND status='Pendente') INSERT INTO dbo.SolicitacoesRecuperacaoSenha(pessoa_id,status) VALUES (@idJoao,'Pendente');
IF NOT EXISTS (SELECT 1 FROM dbo.SolicitacoesRecuperacaoSenha WHERE pessoa_id=@idMaria AND status='Pendente') INSERT INTO dbo.SolicitacoesRecuperacaoSenha(pessoa_id,status) VALUES (@idMaria,'Pendente');
IF NOT EXISTS (SELECT 1 FROM dbo.SolicitacoesRecuperacaoSenha WHERE pessoa_id=@idPedro AND status='Pendente') INSERT INTO dbo.SolicitacoesRecuperacaoSenha(pessoa_id,status) VALUES (@idPedro,'Pendente');

COMMIT TRANSACTION;
SELECT 'Carga demonstrativa concluída' AS resultado;
GO
