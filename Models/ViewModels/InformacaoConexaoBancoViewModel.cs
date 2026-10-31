namespace SIVAD.Models.ViewModels;

public sealed class InformacaoConexaoBancoViewModel
{
    public string Servidor { get; set; } = string.Empty;
    public string Banco { get; set; } = string.Empty;
    public int Produtos { get; set; }
    public int FuncionariosAtivos { get; set; }
    public int Pedidos { get; set; }
    public int Compras { get; set; }
}
