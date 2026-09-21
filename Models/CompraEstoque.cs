public class CompraEstoque
{
    public int Numero { get; set; }
    public DateTime DataCompra { get; set; }
    public decimal Total { get; set; }
    public int Status { get; set; }

    public int AdministradorId { get; set; }
    public Administrador Administrador { get; set; }

    public int FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; }

    public ICollection<ItemCompraEstoque> Itens { get; set; }
}