using Microsoft.Data.SqlClient;

namespace SIVAD.Repositories;

/// <summary>
/// Classe-base dos repositórios SQL. Cada operação cria e descarta sua própria conexão.
/// </summary>
public abstract class DataConnection
{
    private readonly string _stringConexao;

    protected DataConnection(IConfiguration configuracao)
    {
        // A cadeia de conexão vem do appsettings.json e fica centralizada nesta classe-base.
        var stringConexao = configuracao.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(stringConexao))
            throw new InvalidOperationException(
                "Configure ConnectionStrings:DefaultConnection em appsettings.json.");

        _stringConexao = stringConexao;
    }

    // Cada método do repositório abre a conexão quando precisa e a descarta ao terminar.
    protected SqlConnection CriarConexao() => new(_stringConexao);
}
