using SIVAD.Models;

namespace SIVAD.Models.ViewModels;

public sealed class CentralNotificacoesViewModel
{
    public List<Produto> ProdutosEstoqueBaixo { get; set; } = [];
    public List<SolicitacaoRecuperacaoSenhaViewModel> RecuperacoesPendentes { get; set; } = [];
    public int TotalNotificacoes => ProdutosEstoqueBaixo.Count + RecuperacoesPendentes.Count;
}
