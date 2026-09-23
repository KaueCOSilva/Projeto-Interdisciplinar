using SIVAD.Models;

namespace SIVAD.Strategies
{
    public class TotalPedidoStrategy : ICalculoTotalStrategy
    {
        public float CalcularTotal(object entidade)
        {
            var pedido = (Pedido)entidade;
            // Insira aqui a lógica de cálculo do pedido
            return 0; 
        }
    }
}