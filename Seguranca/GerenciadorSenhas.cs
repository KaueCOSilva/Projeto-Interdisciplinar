using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SIVAD.Seguranca;

/// <summary>Gera e confere hashes de senha com salt individual e comparação em tempo constante.</summary>
public static class GerenciadorSenhas
{
    private const string NomeAlgoritmo = "PBKDF2-SHA256";
    private const int IteracoesPadrao = 210_000;
    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;

    public static string CriarHash(string senha)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senha);
        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(senha), salt, IteracoesPadrao,
            HashAlgorithmName.SHA256, TamanhoHash);

        return string.Join('$', NomeAlgoritmo,
            IteracoesPadrao.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public static bool VerificarSenha(string senha, string hashArmazenado)
    {
        if (string.IsNullOrEmpty(senha) || string.IsNullOrWhiteSpace(hashArmazenado))
            return false;

        var partes = hashArmazenado.Split('$');
        if (partes.Length != 4 || partes[0] != NomeAlgoritmo ||
            !int.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iteracoes) ||
            iteracoes < 100_000 || iteracoes > 2_000_000)
            return false;

        try
        {
            var salt = Convert.FromBase64String(partes[2]);
            var hashEsperado = Convert.FromBase64String(partes[3]);
            if (salt.Length < TamanhoSalt || hashEsperado.Length != TamanhoHash)
                return false;

            var hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(senha), salt, iteracoes,
                HashAlgorithmName.SHA256, hashEsperado.Length);
            return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
