/*
    Redefine as credenciais demonstrativas para permitir o primeiro acesso.
    Execute no banco SQLVendas2026 depois de Banco.sql/AtualizarBanco.sql/Insercoes.sql.
    As senhas não ficam gravadas em texto claro: somente os hashes PBKDF2 são atualizados.

    Administrador:
      UID: consultar no SELECT final
      E-mail: admin1@example.invalid
      Senha: Sivad@2026!

    Funcionário:
      UID: consultar no SELECT final
      E-mail: funcionario1@example.invalid
      Senha: Funcionario@2026!
*/
USE SQLVendas2026;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @hashAdmin VARCHAR(200) = 'PBKDF2-SHA256$210000$xrUURskNBAJT7euUhXCarg==$PQk+Fa57IeKcWNJ3N7YsL5wWnkPjlvCgfl8pKmGGI5A=';
DECLARE @hashFuncionario VARCHAR(200) = 'PBKDF2-SHA256$210000$ZfAuUlGhS2cuQwUrUH8cXw==$JzHWTBl8GA4i0ebvv5IclQ0rnRaT85FCeKplqjRD05c=';

UPDATE c
SET c.senha_hash = @hashAdmin,
    c.ativo = 1,
    c.email = 'admin1@example.invalid',
    c.perfil = 'Administrador'
FROM dbo.CredenciaisAcesso c
INNER JOIN dbo.Pessoas p ON p.id = c.pessoa_id
WHERE p.nome = 'Ana Administradora';

IF @@ROWCOUNT <> 1
    THROW 51001, 'Ana Administradora não foi encontrada em CredenciaisAcesso. Execute Insercoes.sql antes deste arquivo.', 1;

UPDATE c
SET c.senha_hash = @hashFuncionario,
    c.ativo = 1,
    c.email = 'funcionario1@example.invalid',
    c.perfil = 'Funcionario'
FROM dbo.CredenciaisAcesso c
INNER JOIN dbo.Pessoas p ON p.id = c.pessoa_id
WHERE p.nome = 'Joao Funcionario';

IF @@ROWCOUNT <> 1
    THROW 51002, 'Joao Funcionario não foi encontrado em CredenciaisAcesso. Execute Insercoes.sql antes deste arquivo.', 1;

COMMIT TRANSACTION;

SELECT p.id AS UID, p.nome, c.email, c.perfil, c.ativo
FROM dbo.Pessoas p
INNER JOIN dbo.CredenciaisAcesso c ON c.pessoa_id = p.id
WHERE p.nome IN ('Ana Administradora', 'Joao Funcionario');
GO
