using System.Data;
using Microsoft.Data.SqlClient;
using SIVAD.Models;
using SIVAD.Models.ViewModels;

namespace SIVAD.Repositories;

/// <summary>Consulta somente leitura para diagnóstico da conexão com o banco.</summary>
public sealed partial class RepositorioSqlServer
{
    // Diagnóstico somente leitura, usado em Development para confirmar servidor e banco ativos.
    public async Task<DatabaseConnectionInfoViewModel> ObterDiagnosticoBancoAsync()
    {
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        const string sql = "SELECT CAST(SERVERPROPERTY('ServerName') AS nvarchar(128)), DB_NAME(), (SELECT COUNT(*) FROM Produtos), (SELECT COUNT(*) FROM Funcionarios WHERE ativo=1), (SELECT COUNT(*) FROM Pedidos), (SELECT COUNT(*) FROM CompraEstoque)";
        await using var comando = new SqlCommand(sql, conexao);
        await using var leitor = await comando.ExecuteReaderAsync();
        if (!await leitor.ReadAsync()) throw new InvalidOperationException("O SQL Server não retornou informações do banco conectado.");
        return new DatabaseConnectionInfoViewModel
        {
            Servidor = leitor.GetString(0),
            Banco = leitor.GetString(1),
            Produtos = leitor.GetInt32(2),
            FuncionariosAtivos = leitor.GetInt32(3),
            Pedidos = leitor.GetInt32(4),
            Compras = leitor.GetInt32(5)
        };
    }
}
