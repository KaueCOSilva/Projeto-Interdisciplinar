using System.Collections.Generic;

namespace SIVAD.Models
{
    public class Produto
    {
        public int Codigo { get; set; }
        public string CodigoBarras { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public decimal PrecoUnit { get; set; }
        public int Estoque { get; set; }

        public int CategoriaCodigo { get; set; }
        public Categoria Categoria { get; set; } = null!;

        public ICollection<ItemCompraEstoque> ItensCompraEstoque { get; set; } = new List<ItemCompraEstoque>();
        public ICollection<ItemPedido> ItensPedidos { get; set; } = new List<ItemPedido>();
    }
}