using SIVAD.Models;

namespace SIVAD.Strategies
{
    public class TotalCompraEstoqueStrategy : ICalculoTotalStrategy<CompraEstoque>
    {
        // A Strategy isola a regra de cálculo do controller e do SQL.
        // ItemCompraEstoque.Valor já é o valor total da linha (quantidade x custo unitário,
        // conforme Banco.sql), então o total da compra é a soma direta.
        public decimal CalcularTotal(CompraEstoque compra)
        {
            if (compra?.Itens == null) return 0m;

            // Cada item já contém o total da linha; aqui apenas agregamos os itens.
            return compra.Itens.Sum(item => item.Valor);
        }
    }
}
