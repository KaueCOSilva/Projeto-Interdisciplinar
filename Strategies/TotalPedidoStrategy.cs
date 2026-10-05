using SIVAD.Models;

namespace SIVAD.Strategies
{
    public class TotalPedidoStrategy : ICalculoTotalStrategy<Pedido>
    {
        // ItemPedido.PrecoTotal já é o total da linha (preco_unit x qtd, conforme Banco.sql),
        // então o total do pedido é a soma direta, sem multiplicar pela quantidade de novo.
        public decimal CalcularTotal(Pedido pedido)
        {
            if (pedido?.Itens == null) return 0m;

            return pedido.Itens.Sum(item => item.PrecoTotal);
        }
    }
}
