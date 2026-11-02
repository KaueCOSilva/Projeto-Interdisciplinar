using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SIVAD.Models
{
    public class ItemPedido
    {
        public int PedidoCodigo { get; set; }
        [ValidateNever]
        public Pedido Pedido { get; set; } = null!;

        public int ProdutoCodigo { get; set; }
        [ValidateNever]
        public Produto Produto { get; set; } = null!;

        public int Qtd { get; set; }
        public decimal PrecoTotal { get; set; }
    }
}
