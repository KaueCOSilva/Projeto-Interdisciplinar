using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SIVAD.Models
{
    public class Produto
    {
        public int Codigo { get; set; }
        public string CodigoBarras { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Cor { get; set; } = "Não informada";
        public string FornecedorNome { get; set; } = string.Empty;
        public decimal PrecoUnit { get; set; }
        public int Estoque { get; set; }
        public bool Ativo { get; set; } = true;

        public int CategoriaCodigo { get; set; }
        [ValidateNever]
        public Categoria Categoria { get; set; } = null!;

        public ICollection<ItemCompraEstoque> ItensCompraEstoque { get; set; } = new List<ItemCompraEstoque>();
        public ICollection<ItemPedido> ItensPedidos { get; set; } = new List<ItemPedido>();
    }
}
