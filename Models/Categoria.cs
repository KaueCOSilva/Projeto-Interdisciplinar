using System.Collections.Generic;

namespace SIVAD.Models
{
    public class Categoria
    {
        public int Codigo { get; set; }
        public string Nome { get; set; } = string.Empty;

        public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
    }
}