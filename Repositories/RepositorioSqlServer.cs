using System.Data;
using Microsoft.Data.SqlClient;
using SIVAD.Models;
using SIVAD.Models.ViewModels;

namespace SIVAD.Repositories;

/// <summary>Consultas e comandos SQL parametrizados usados pelos controladores MVC.</summary>
public sealed partial class RepositorioSqlServer : DataConnection, IRepositorioSIVAD
{
    public RepositorioSqlServer(IConfiguration configuracao) : base(configuracao) { }


    // Helpers de segurança: todos os valores entram como parâmetros, nunca por concatenação de strings.
    private static void AdicionarInteiro(SqlCommand comando, string nomeParametro, int valor) => comando.Parameters.Add(nomeParametro, SqlDbType.Int).Value = valor;
    private static void AdicionarValorMonetario(SqlCommand comando, string nomeParametro, decimal valor)
    {
        // Precisão e escala seguem o formato monetário definido no banco.
        var parametro = comando.Parameters.Add(nomeParametro, SqlDbType.Decimal);
        parametro.Precision = 10; parametro.Scale = 2; parametro.Value = valor;
    }
}
