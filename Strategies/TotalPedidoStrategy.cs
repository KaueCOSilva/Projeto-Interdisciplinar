using SIVAD.Models;

namespace SIVAD.Strategies
{
    public class TotalPedidoStrategy : ICalculoTotalStrategy
    {
        public float CalcularTotal(object entidade)
        {
            var pedido = (Pedido)entidade;
            if (pedido?.Itens == null) return 0f;

            float total = 0f;
            foreach (var item in pedido.Itens)
            {
                total += (float)item.PrecoTotal * item.Qtd;
            }
            return total;
        }
    }
}