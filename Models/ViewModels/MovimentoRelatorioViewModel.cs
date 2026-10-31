namespace SIVAD.Models.ViewModels;

public sealed class MovimentoRelatorioViewModel
{
    public DateTime Data { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int Quantidade { get; set; }
}
