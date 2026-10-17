namespace SIVAD.Models.ViewModels;

public sealed class StockViewModel
{
    public List<Produto> Produtos { get; set; } = [];
    public List<Categoria> Categorias { get; set; } = [];
}
