using SIVAD.Models;

namespace SIVAD.Strategies
{
    public class TotalCompraEstoqueStrategy : ICalculoTotalStrategy
    {
        public float CalcularTotal(object entidade)
        {
            var compra = (CompraEstoque)entidade;
            if (compra?.Itens == null) return 0f;

            float total = 0f;
            foreach (var item in compra.Itens)
            {
                total += (float)item.Valor * item.Quantidade;
            }
            return total;
        }
    }
}