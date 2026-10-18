using SIVAD.Models;

namespace SIVAD.Models.ViewModels;

public sealed class EmployeeManagementViewModel
{
    public List<Funcionario> Funcionarios { get; set; } = [];
    public int? FuncionarioEmEdicaoId { get; set; }
    public string? FuncionarioEmEdicaoNome { get; set; }
}
