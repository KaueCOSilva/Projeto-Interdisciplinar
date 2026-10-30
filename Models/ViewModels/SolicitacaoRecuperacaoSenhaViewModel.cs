namespace SIVAD.Models.ViewModels;

public sealed class SolicitacaoRecuperacaoSenhaViewModel
{
    public int SolicitacaoId { get; set; }
    public int PessoaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DataSolicitacao { get; set; }
}
