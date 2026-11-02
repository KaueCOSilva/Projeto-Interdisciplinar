using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SIVAD.Models
{
    public class CompraEstoque
    {
        public int Numero { get; set; }
        public DateTime DataCompra { get; set; }
        public decimal Total { get; set; }
        public int Status { get; set; }

        public int AdministradorId { get; set; }
        [ValidateNever]
        public Administrador Administrador { get; set; } = null!;

        public int? FornecedorId { get; set; }
        public string FornecedorNome { get; set; } = string.Empty;
        [ValidateNever]
        public Fornecedor? Fornecedor { get; set; }

        public ICollection<ItemCompraEstoque> Itens { get; set; } = new List<ItemCompraEstoque>();
    }
}
