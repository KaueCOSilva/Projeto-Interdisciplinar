namespace SIVAD.Models;

/// <summary>Dados de autenticação lidos da tabela CredenciaisAcesso.</summary>
public sealed class CredencialAcesso
{
    public int PessoaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
}
