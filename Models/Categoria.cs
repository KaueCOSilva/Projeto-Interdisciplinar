public class Categoria
{
    public int Codigo { get; set; }
    public string Nome { get; set; }

    public ICollection<Produto> Produtos { get; set; }
}