using SIVAD.Models;

namespace SIVAD.Models.ViewModels;

public sealed class CompraEstoqueViewModel
{
    public List<Produto> Produtos { get; set; } = [];
    public List<Administrador> Administradores { get; set; } = [];
    public List<CompraEstoque> HistoricoCompras { get; set; } = [];
    public CompraEstoque? CompraEmEdicao { get; set; }
}
