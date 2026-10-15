namespace SIVAD.Models
{
    public class ItemCompraEstoque
    {
        public int CompraNumero { get; set; }
        public CompraEstoque CompraEstoque { get; set; } = null!;

        public int ProdutoCodigo { get; set; }
        public Produto Produto { get; set; } = null!;

        public int Quantidade { get; set; }
        public string Cor { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        // Campo recebido do formulário; Valor é o total da linha armazenado no banco.
        public decimal PrecoUnitario { get; set; }
    }
}
