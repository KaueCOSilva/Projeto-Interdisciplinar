public class ItemPedido
{
    public int PedidoCodigo { get; set; }
    public Pedido Pedido { get; set; }

    public int ProdutoCodigo { get; set; }
    public Produto Produto { get; set; }

    public int Qtd { get; set; }
    public decimal PrecoTotal { get; set; }
}