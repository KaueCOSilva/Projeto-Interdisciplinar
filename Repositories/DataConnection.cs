using Microsoft.Data.SqlClient;

namespace SIVAD.Repositories;

public abstract class DataConnection
{
    private readonly string _connectionString;

    protected DataConnection(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Configure ConnectionStrings:DefaultConnection em appsettings.json.");

        _connectionString = connectionString;
    }

    protected SqlConnection CriarConexao() => new(_connectionString);
}
