namespace SIVAD.Models
{
    public class ItemPedido
    {
        public int PedidoCodigo { get; set; }
        public Pedido Pedido { get; set; } = null!;

        public int ProdutoCodigo { get; set; }
        public Produto Produto { get; set; } = null!;

        public int Qtd { get; set; }
        public decimal PrecoTotal { get; set; }
    }
}