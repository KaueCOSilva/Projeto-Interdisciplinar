namespace SIVAD.Strategies
{
    // Strategy de cálculo de total: cada implementação sabe somar um tipo de entidade.
    // Usa genérico (sem cast de object) e decimal (adequado para valores monetários).
    public interface ICalculoTotalStrategy<in T>
    {
        decimal CalcularTotal(T entidade);
    }
}
