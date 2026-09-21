public class Pedido
{
    public int Codigo { get; set; }
    public int Status { get; set; }
    public decimal ValorTotal { get; set; }

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; }

    public int FuncionarioId { get; set; }
    public Funcionario Funcionario { get; set; }

    public ICollection<ItemPedido> Itens { get; set; }
}