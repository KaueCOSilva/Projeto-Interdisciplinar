using Microsoft.Data.SqlClient;

namespace SIVAD.Repositories;

/// <summary>
/// Classe-base dos repositories SQL. Cada operação cria e descarta sua própria conexão.
/// </summary>
public abstract class DataConnection
{
    private readonly string _connectionString;

    protected DataConnection(IConfiguration configuration)
    {
        // A connection string vem do appsettings.json e não fica espalhada nos controllers.
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Configure ConnectionStrings:DefaultConnection em appsettings.json.");

        _connectionString = connectionString;
    }

    // Cada método do Repository abre a conexão quando precisa e a descarta ao terminar.
    protected SqlConnection CriarConexao() => new(_connectionString);
}
