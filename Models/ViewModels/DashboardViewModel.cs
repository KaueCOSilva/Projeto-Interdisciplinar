using SIVAD.Models;

namespace SIVAD.Models.ViewModels;

public sealed class DashboardViewModel
{
    public decimal VendasTotais { get; set; }
    public decimal VendasDoMes { get; set; }
    public int UnidadesVendidas { get; set; }
    public int UnidadesDevolvidas { get; set; }
    public int AlertasEstoque { get; set; }
    public List<VendaMensalViewModel> VendasPorMes { get; set; } = [];
    public List<CompraAnualViewModel> ComprasPorAno { get; set; } = [];
    public List<VendaPorCategoriaViewModel> VendasPorCategoria { get; set; } = [];
    public List<AtividadeViewModel> Atividades { get; set; } = [];
    public List<Produto> EstoqueBaixo { get; set; } = [];
}
