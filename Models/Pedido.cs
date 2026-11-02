using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SIVAD.Models
{
    public class Pedido
    {
        public int Codigo { get; set; }
        public int Status { get; set; }
        public decimal ValorTotal { get; set; }

        public int? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public int FuncionarioId { get; set; }
        [ValidateNever]
        public Funcionario Funcionario { get; set; } = null!;

        public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
    }
}
