namespace SIVAD.Models.ViewModels;

public sealed class AtividadeViewModel
{
    public string Descricao { get; set; } = string.Empty;
    public DateTime Data { get; set; }
    public string Tipo { get; set; } = "info";
}
