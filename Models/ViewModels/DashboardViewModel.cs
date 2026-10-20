namespace SIVAD.Models.ViewModels;

public sealed class DashboardViewModel
{
    public decimal VendasTotais { get; set; }
    public decimal VendasDoMes { get; set; }
    public int UnidadesVendidas { get; set; }
    public int UnidadesDevolvidas { get; set; }
    public int AlertasEstoque { get; set; }
    public List<MonthlySalesViewModel> VendasPorMes { get; set; } = [];
    public List<AnnualPurchaseViewModel> ComprasPorAno { get; set; } = [];
    public List<CategorySalesViewModel> VendasPorCategoria { get; set; } = [];
    public List<ActivityViewModel> Atividades { get; set; } = [];
    public List<Produto> EstoqueBaixo { get; set; } = [];
}

public sealed class MonthlySalesViewModel
{
    public string Label { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal Compras { get; set; }
}

public sealed class AnnualPurchaseViewModel
{
    public int Ano { get; set; }
    public decimal Total { get; set; }
}

public sealed class CategorySalesViewModel
{
    public string Categoria { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}

public sealed class ActivityViewModel
{
    public string Descricao { get; set; } = string.Empty;
    public DateTime Data { get; set; }
    public string Tipo { get; set; } = "info";
}

public sealed class ReportViewModel
{
    public List<ReportMovementViewModel> Movimentos { get; set; } = [];
}

public sealed class ReportMovementViewModel
{
    public DateTime Data { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int Quantidade { get; set; }
}

public sealed class EmployeeOrderViewModel
{
    public List<Produto> Produtos { get; set; } = [];
    public List<Funcionario> Funcionarios { get; set; } = [];
}

public sealed class StockPurchaseViewModel
{
    public List<Produto> Produtos { get; set; } = [];
    public List<Administrador> Administradores { get; set; } = [];
    public List<CompraEstoque> HistoricoCompras { get; set; } = [];
    public CompraEstoque? CompraEmEdicao { get; set; }
}
