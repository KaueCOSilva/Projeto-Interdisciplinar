using Microsoft.AspNetCore.Mvc;
using SIVAD.Repositories;
using SIVAD.Filtros;

namespace SIVAD.Controllers;

[Route("diagnostico/banco")]
[ExigirSessao("Administrador")]
public sealed class DiagnosticoController : Controller
{
    private readonly IRepositorioSIVAD _repository;
    private readonly IWebHostEnvironment _environment;

    public DiagnosticoController(IRepositorioSIVAD repository, IWebHostEnvironment environment)
    {
        _repository = repository;
        _environment = environment;
    }

    [HttpGet]
    // Endpoint somente leitura e restrito a Development; não altera dados.
    public async Task<IActionResult> Index()
    {
        if (!_environment.IsDevelopment()) return NotFound();
        try { return Json(await _repository.ObterDiagnosticoBancoAsync()); }
        catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException or InvalidOperationException)
        {
            return Problem(title: "Falha ao verificar o SQL Server", detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
