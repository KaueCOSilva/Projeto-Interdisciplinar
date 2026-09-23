namespace SIVAD.Models
{
    public class ItemCompraEstoque
    {
        public int CompraNumero { get; set; }
        public CompraEstoque CompraEstoque { get; set; } = null!;

        public int ProdutoCodigo { get; set; }
        public Produto Produto { get; set; } = null!;

        public int Quantidade { get; set; }
        public decimal Valor { get; set; }
    }
}