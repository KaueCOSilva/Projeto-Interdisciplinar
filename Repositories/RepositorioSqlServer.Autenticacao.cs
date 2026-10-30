using System.Data;
using Microsoft.Data.SqlClient;
using SIVAD.Models;
using SIVAD.Models.ViewModels;

namespace SIVAD.Repositories;

/// <summary>Consultas e gravações de acesso e recuperação de senha.</summary>
public sealed partial class RepositorioSqlServer
{
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

    // O primeiro acesso administrativo só é criado se CPF e e-mail correspondem ao cadastro existente.
    public async Task<bool> ConfigurarPrimeiroAdministradorAsync(int pessoaId, string cpf, string email, string senhaHash)
    {
        if (pessoaId <= 0 || string.IsNullOrWhiteSpace(cpf) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senhaHash))
            return false;

        var cpfNormalizado = cpf.Trim().Replace(".", "").Replace("-", "").Replace(" ", "");
        var emailNormalizado = email.Trim().ToLowerInvariant();
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync(IsolationLevel.Serializable);

        await using (var verificar = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.CredenciaisAcesso WHERE perfil = 'Administrador'", conexao, transacao))
        {
            if (Convert.ToInt32(await verificar.ExecuteScalarAsync()) != 0)
                return false;
        }

        const string sql = @"
INSERT INTO dbo.CredenciaisAcesso (pessoa_id, perfil, email, senha_hash, ativo)
SELECT a.pessoa_id, 'Administrador', @email, @senhaHash, 1
FROM dbo.Administradores a
WHERE a.pessoa_id = @pessoaId
  AND REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(a.cpf)), '.', ''), '-', ''), ' ', '') = @cpf
  AND LOWER(LTRIM(RTRIM(a.email))) = @email
  AND NOT EXISTS (SELECT 1 FROM dbo.CredenciaisAcesso c WHERE c.pessoa_id = a.pessoa_id)";
        await using var comando = new SqlCommand(sql, conexao, transacao);
        AdicionarInteiro(comando, "@pessoaId", pessoaId);
        comando.Parameters.Add("@cpf", SqlDbType.VarChar, 20).Value = cpfNormalizado;
        comando.Parameters.Add("@email", SqlDbType.VarChar, 254).Value = emailNormalizado;
        comando.Parameters.Add("@senhaHash", SqlDbType.VarChar, 200).Value = senhaHash;
        var inseridos = await comando.ExecuteNonQueryAsync();
        if (inseridos != 1) return false;

        await transacao.CommitAsync();
        return true;
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
      SELECT 1 FROM dbo.SolicitacoesRecuperacaoSenha s
      WHERE s.pessoa_id = c.pessoa_id AND s.status = 'Pendente')";
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        AdicionarInteiro(comando, "@pessoaId", pessoaId);
        comando.Parameters.Add("@nome", SqlDbType.VarChar, 150).Value = nome.Trim();
        comando.Parameters.Add("@email", SqlDbType.VarChar, 254).Value = email.Trim();
        return await comando.ExecuteNonQueryAsync() == 1;
    }

    public async Task<List<SolicitacaoRecuperacaoSenhaViewModel>> ListarSolicitacoesRecuperacaoSenhaAsync()
    {
        var lista = new List<SolicitacaoRecuperacaoSenhaViewModel>();
        const string sql = @"
SELECT s.solicitacao_id, s.pessoa_id, p.nome, c.email, s.data_solicitacao
FROM dbo.SolicitacoesRecuperacaoSenha s
INNER JOIN dbo.Pessoas p ON p.id = s.pessoa_id
INNER JOIN dbo.CredenciaisAcesso c ON c.pessoa_id = s.pessoa_id AND c.perfil = 'Funcionario' AND c.ativo = 1
INNER JOIN dbo.Funcionarios f ON f.pessoa_id = s.pessoa_id AND f.ativo = 1
WHERE s.status = 'Pendente'
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
