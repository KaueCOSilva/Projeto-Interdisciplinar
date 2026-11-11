using System.Data;
using Microsoft.Data.SqlClient;
using SIVAD.Models;
using SIVAD.Models.ViewModels;

namespace SIVAD.Repositories;

/// <summary>Consultas e gravações de acesso e recuperação de senha.</summary>
public sealed partial class RepositorioSqlServer
{
    public async Task<bool> ExisteAdministradorConfiguradoAsync()
    {
        const string sql = "SELECT COUNT(*) FROM dbo.CredenciaisAcesso WHERE perfil='Administrador'";
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        return Convert.ToInt32(await comando.ExecuteScalarAsync()) > 0;
    }

    public async Task<CredencialAcesso?> ObterCredencialAcessoAsync(int pessoaId)
    {
        if (pessoaId <= 0) return null;

        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        const string sql = @"
SELECT c.pessoa_id, p.nome, c.email, c.perfil, c.senha_hash
FROM dbo.CredenciaisAcesso c
INNER JOIN dbo.Pessoas p ON p.id = c.pessoa_id
WHERE c.pessoa_id = @pessoaId
  AND c.ativo = 1
  AND (
      (c.perfil = 'Administrador' AND EXISTS (
          SELECT 1 FROM dbo.Administradores a WHERE a.pessoa_id = c.pessoa_id))
      OR
      (c.perfil = 'Funcionario' AND EXISTS (
          SELECT 1 FROM dbo.Funcionarios f WHERE f.pessoa_id = c.pessoa_id AND f.ativo = 1))
  )";
        await using var comando = new SqlCommand(sql, conexao);
        AdicionarInteiro(comando, "@pessoaId", pessoaId);
        await using var leitor = await comando.ExecuteReaderAsync();
        if (!await leitor.ReadAsync()) return null;

        return new CredencialAcesso
        {
            PessoaId = leitor.GetInt32(0),
            Nome = leitor.GetString(1),
            Email = leitor.GetString(2),
            Perfil = leitor.GetString(3),
            SenhaHash = leitor.GetString(4)
        };
    }

    // Cria o primeiro administrador nas tabelas previstas pelo modelo relacional.
    // Se o cadastro administrativo já existir sem credenciais, permite vinculá-lo também.
    public async Task<int?> CadastrarPrimeiroAdministradorAsync(string nome, string cpf, string email, string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(cpf) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senhaHash))
            return null;

        var nomeNormalizado = nome.Trim();
        var cpfNormalizado = new string(cpf.Where(caractere => caractere >= '0' && caractere <= '9').ToArray());
        var emailNormalizado = email.Trim().ToLowerInvariant();
        if (nomeNormalizado.Length > 150 || cpfNormalizado.Length != 11 || emailNormalizado.Length > 100)
            return null;

        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync(IsolationLevel.Serializable);

        // A transação serializável impede que duas primeiras configurações sejam concluídas em paralelo.
        await using (var verificarAdministrador = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.CredenciaisAcesso WITH (UPDLOCK, HOLDLOCK) WHERE perfil = 'Administrador'",
            conexao, transacao))
        {
            if (Convert.ToInt32(await verificarAdministrador.ExecuteScalarAsync()) != 0)
                return null;
        }

        // O e-mail de acesso precisa ser único entre funcionários e administradores.
        await using (var verificarEmail = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.CredenciaisAcesso WITH (UPDLOCK, HOLDLOCK) WHERE LOWER(LTRIM(RTRIM(email))) = @email",
            conexao, transacao))
        {
            verificarEmail.Parameters.Add("@email", SqlDbType.VarChar, 254).Value = emailNormalizado;
            if (Convert.ToInt32(await verificarEmail.ExecuteScalarAsync()) != 0)
                return null;
        }

        // Mantém compatibilidade com um administrador de negócio previamente cadastrado,
        // desde que nome, CPF e e-mail coincidam e ele ainda não tenha credenciais.
        const string localizarAdministrador = @"
SELECT TOP (1) a.pessoa_id
FROM dbo.Administradores a WITH (UPDLOCK, HOLDLOCK)
INNER JOIN dbo.Pessoas p ON p.id = a.pessoa_id
WHERE REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(a.cpf)), '.', ''), '-', ''), ' ', '') = @cpf
  AND LOWER(LTRIM(RTRIM(a.email))) = @email
  AND LOWER(LTRIM(RTRIM(p.nome))) = LOWER(@nome)";
        int? pessoaId = null;
        await using (var localizar = new SqlCommand(localizarAdministrador, conexao, transacao))
        {
            localizar.Parameters.Add("@cpf", SqlDbType.VarChar, 14).Value = cpfNormalizado;
            localizar.Parameters.Add("@email", SqlDbType.VarChar, 100).Value = emailNormalizado;
            localizar.Parameters.Add("@nome", SqlDbType.VarChar, 150).Value = nomeNormalizado;
            var resultado = await localizar.ExecuteScalarAsync();
            if (resultado is not null && resultado != DBNull.Value)
                pessoaId = Convert.ToInt32(resultado);
        }

        if (pessoaId.HasValue)
        {
            await using var verificarCredencialExistente = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.CredenciaisAcesso WITH (UPDLOCK, HOLDLOCK) WHERE pessoa_id = @pessoaId",
                conexao, transacao);
            AdicionarInteiro(verificarCredencialExistente, "@pessoaId", pessoaId.Value);
            if (Convert.ToInt32(await verificarCredencialExistente.ExecuteScalarAsync()) != 0)
                return null;
        }
        else
        {
            // Evita duplicar um CPF ou e-mail que já pertença a outro administrador cadastrado.
            const string verificarDadosExistentes = @"
SELECT COUNT(*)
FROM dbo.Administradores WITH (UPDLOCK, HOLDLOCK)
WHERE REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(cpf)), '.', ''), '-', ''), ' ', '') = @cpf
   OR LOWER(LTRIM(RTRIM(email))) = @email";
            await using (var verificarDados = new SqlCommand(verificarDadosExistentes, conexao, transacao))
            {
                verificarDados.Parameters.Add("@cpf", SqlDbType.VarChar, 14).Value = cpfNormalizado;
                verificarDados.Parameters.Add("@email", SqlDbType.VarChar, 100).Value = emailNormalizado;
                if (Convert.ToInt32(await verificarDados.ExecuteScalarAsync()) != 0)
                    return null;
            }

            await using (var inserirPessoa = new SqlCommand(
                "INSERT INTO dbo.Pessoas (nome) VALUES (@nome); SELECT CONVERT(INT, SCOPE_IDENTITY());",
                conexao, transacao))
            {
                inserirPessoa.Parameters.Add("@nome", SqlDbType.VarChar, 150).Value = nomeNormalizado;
                pessoaId = Convert.ToInt32(await inserirPessoa.ExecuteScalarAsync());
            }

            await using (var inserirAdministrador = new SqlCommand(
                "INSERT INTO dbo.Administradores (pessoa_id, cpf, email) VALUES (@pessoaId, @cpf, @email)",
                conexao, transacao))
            {
                AdicionarInteiro(inserirAdministrador, "@pessoaId", pessoaId.Value);
                inserirAdministrador.Parameters.Add("@cpf", SqlDbType.VarChar, 14).Value = cpfNormalizado;
                inserirAdministrador.Parameters.Add("@email", SqlDbType.VarChar, 100).Value = emailNormalizado;
                await inserirAdministrador.ExecuteNonQueryAsync();
            }
        }

        await using (var inserirCredencial = new SqlCommand(@"
INSERT INTO dbo.CredenciaisAcesso (pessoa_id, perfil, email, senha_hash, ativo)
VALUES (@pessoaId, 'Administrador', @email, @senhaHash, 1)", conexao, transacao))
        {
            AdicionarInteiro(inserirCredencial, "@pessoaId", pessoaId.Value);
            inserirCredencial.Parameters.Add("@email", SqlDbType.VarChar, 254).Value = emailNormalizado;
            inserirCredencial.Parameters.Add("@senhaHash", SqlDbType.VarChar, 200).Value = senhaHash;
            await inserirCredencial.ExecuteNonQueryAsync();
        }

        await transacao.CommitAsync();
        return pessoaId;
    }

    // Cria ou atualiza o acesso de um funcionário ativo, sempre usando o ID da Pessoa como UID.
    public async Task<bool> ConfigurarAcessoFuncionarioAsync(int pessoaId, string email, string senhaHash)
    {
        if (pessoaId <= 0 || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senhaHash))
            return false;

        var emailNormalizado = email.Trim().ToLowerInvariant();
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync();

        await using (var verificarFuncionario = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Funcionarios WHERE pessoa_id=@pessoaId AND ativo=1", conexao, transacao))
        {
            AdicionarInteiro(verificarFuncionario, "@pessoaId", pessoaId);
            if (Convert.ToInt32(await verificarFuncionario.ExecuteScalarAsync()) != 1)
                return false;
        }

        string? perfilExistente = null;
        await using (var verificarAcesso = new SqlCommand(
            "SELECT perfil FROM dbo.CredenciaisAcesso WHERE pessoa_id=@pessoaId", conexao, transacao))
        {
            AdicionarInteiro(verificarAcesso, "@pessoaId", pessoaId);
            perfilExistente = await verificarAcesso.ExecuteScalarAsync() as string;
        }

        if (perfilExistente is not null && perfilExistente != "Funcionario")
            return false;

        if (perfilExistente == "Funcionario")
        {
            const string atualizarSql = @"UPDATE dbo.CredenciaisAcesso
SET email=@email, senha_hash=@senhaHash, ativo=1
WHERE pessoa_id=@pessoaId AND perfil='Funcionario'";
            await using var atualizar = new SqlCommand(atualizarSql, conexao, transacao);
            AdicionarInteiro(atualizar, "@pessoaId", pessoaId);
            atualizar.Parameters.Add("@email", SqlDbType.VarChar, 254).Value = emailNormalizado;
            atualizar.Parameters.Add("@senhaHash", SqlDbType.VarChar, 200).Value = senhaHash;
            if (await atualizar.ExecuteNonQueryAsync() != 1) return false;
        }
        else
        {
            const string inserirSql = @"INSERT INTO dbo.CredenciaisAcesso(pessoa_id,perfil,email,senha_hash,ativo)
VALUES(@pessoaId,'Funcionario',@email,@senhaHash,1)";
            await using var inserir = new SqlCommand(inserirSql, conexao, transacao);
            AdicionarInteiro(inserir, "@pessoaId", pessoaId);
            inserir.Parameters.Add("@email", SqlDbType.VarChar, 254).Value = emailNormalizado;
            inserir.Parameters.Add("@senhaHash", SqlDbType.VarChar, 200).Value = senhaHash;
            await inserir.ExecuteNonQueryAsync();
        }

        await transacao.CommitAsync();
        return true;
    }

    // Confere nome, UID e e-mail do funcionário; a resposta visual não revela se existe.
    public async Task<bool> RegistrarSolicitacaoRecuperacaoSenhaAsync(int pessoaId, string nome, string email)
    {
        if (pessoaId <= 0 || string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email))
            return false;

        const string sql = @"
INSERT INTO dbo.SolicitacoesRecuperacaoSenha(pessoa_id)
SELECT c.pessoa_id
FROM dbo.CredenciaisAcesso c
INNER JOIN dbo.Pessoas p ON p.id = c.pessoa_id
WHERE c.pessoa_id = @pessoaId
  AND c.ativo = 1
  AND LOWER(LTRIM(RTRIM(p.nome))) = LOWER(LTRIM(RTRIM(@nome)))
  AND LOWER(LTRIM(RTRIM(c.email))) = LOWER(LTRIM(RTRIM(@email)))
  AND c.perfil = 'Funcionario'
  AND EXISTS (
      SELECT 1 FROM dbo.Funcionarios f WHERE f.pessoa_id = c.pessoa_id AND f.ativo = 1)
  AND NOT EXISTS (
      SELECT 1 FROM dbo.SolicitacoesRecuperacaoSenha s WITH (UPDLOCK,HOLDLOCK)
      WHERE s.pessoa_id = c.pessoa_id AND s.status = 'Pendente')";
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync(IsolationLevel.Serializable);
        await using var comando = new SqlCommand(sql, conexao, transacao);
        AdicionarInteiro(comando, "@pessoaId", pessoaId);
        comando.Parameters.Add("@nome", SqlDbType.VarChar, 150).Value = nome.Trim();
        comando.Parameters.Add("@email", SqlDbType.VarChar, 254).Value = email.Trim();
        var registrada = await comando.ExecuteNonQueryAsync() == 1;
        await transacao.CommitAsync();
        return registrada;
    }

    public async Task<List<SolicitacaoRecuperacaoSenhaViewModel>> ListarSolicitacoesRecuperacaoSenhaAsync()
    {
        var lista = new List<SolicitacaoRecuperacaoSenhaViewModel>();
        const string sql = @"
	SELECT s.solicitacao_id, s.pessoa_id, s.pessoa_nome, s.email, s.data_solicitacao
	FROM dbo.vw_SolicitacoesRecuperacaoSenha s
	INNER JOIN dbo.Funcionarios f ON f.pessoa_id = s.pessoa_id AND f.ativo = 1
	WHERE s.status = 'Pendente'
	  AND EXISTS (
	      SELECT 1
	      FROM dbo.CredenciaisAcesso c
	      WHERE c.pessoa_id = s.pessoa_id
	        AND c.perfil = 'Funcionario'
	        AND c.ativo = 1)
	ORDER BY s.data_solicitacao";
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync())
        {
            lista.Add(new SolicitacaoRecuperacaoSenhaViewModel
            {
                SolicitacaoId = leitor.GetInt32(0),
                PessoaId = leitor.GetInt32(1),
                Nome = leitor.GetString(2),
                Email = leitor.GetString(3),
                DataSolicitacao = leitor.GetDateTime(4)
            });
        }
        return lista;
    }

    public async Task<bool> RejeitarSolicitacaoRecuperacaoSenhaAsync(int solicitacaoId)
    {
        if (solicitacaoId <= 0) return false;

        const string sql = @"UPDATE dbo.SolicitacoesRecuperacaoSenha
SET status='Rejeitada'
WHERE solicitacao_id=@id AND status='Pendente'";
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        AdicionarInteiro(comando, "@id", solicitacaoId);
        return await comando.ExecuteNonQueryAsync() == 1;
    }
}
