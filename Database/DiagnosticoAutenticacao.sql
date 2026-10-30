-- Execute no SQL Client conectado ao mesmo catálogo usado por DefaultConnection.
-- O script não altera dados nem mostra CPF, e-mail ou senha.

SELECT DB_NAME() AS banco_atual,
       OBJECT_ID(N'dbo.CredenciaisAcesso', N'U') AS tabela_credenciais,
       OBJECT_ID(N'dbo.SolicitacoesRecuperacaoSenha', N'U') AS tabela_solicitacoes,
       COL_LENGTH('dbo.Pessoas', 'id') AS pessoas_id,
       COL_LENGTH('dbo.Pessoas', 'nome') AS pessoas_nome,
       COL_LENGTH('dbo.Administradores', 'pessoa_id') AS administradores_pessoa_id,
       COL_LENGTH('dbo.Administradores', 'cpf') AS administradores_cpf,
       COL_LENGTH('dbo.Administradores', 'email') AS administradores_email,
       COL_LENGTH('dbo.Funcionarios', 'ativo') AS funcionarios_ativo;
GO

IF OBJECT_ID(N'dbo.CredenciaisAcesso', N'U') IS NOT NULL
BEGIN
    SELECT COUNT(*) AS contas_administradoras_configuradas
    FROM dbo.CredenciaisAcesso
    WHERE perfil = 'Administrador';
END
ELSE
BEGIN
    PRINT 'CredenciaisAcesso nao existe neste banco. Execute Database/AtualizarBanco.sql aqui.';
END;
GO
