public class ItemCompraEstoque
{
    public int CompraNumero { get; set; }
    public CompraEstoque CompraEstoque { get; set; }

    public int ProdutoCodigo { get; set; }
    public Produto Produto { get; set; }

    public int Quantidade { get; set; }
    public decimal Valor { get; set; }
}