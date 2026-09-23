public class Produto
{
    public int Codigo { get; set; }
    public string CodigoBarras { get; set; }
    public string Nome { get; set; }
    public decimal PrecoUnit { get; set; }
    public int Estoque { get; set; }

    public int CategoriaCodigo { get; set; }
    public Categoria Categoria { get; set; }

    public ICollection<ItemCompraEstoque> ItensCompraEstoque { get; set; }
    public ICollection<ItemPedido> ItensPedidos { get; set; }
}