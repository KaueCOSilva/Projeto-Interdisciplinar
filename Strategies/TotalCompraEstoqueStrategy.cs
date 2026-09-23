using SIVAD.Models;

namespace SIVAD.Strategies
{
    public class TotalCompraEstoqueStrategy : ICalculoTotalStrategy
    {
        public float CalcularTotal(object entidade)
        {
            var compra = (CompraEstoque)entidade;
            // Insira aqui a lógica de cálculo do estoque
            return 0; 
        }
    }
}