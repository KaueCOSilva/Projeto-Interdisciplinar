using SIVAD.Models;

namespace SIVAD.Models.ViewModels;

public sealed class PedidoFuncionarioViewModel
{
    public List<Produto> Produtos { get; set; } = [];
    public List<Funcionario> Funcionarios { get; set; } = [];
}
