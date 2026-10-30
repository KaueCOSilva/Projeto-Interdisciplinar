using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SIVAD.Filtros;

/// <summary>Exige sessão ativa e, quando informada, restringe o perfil do usuário.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ExigirSessaoAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string? _perfilPermitido;

    public ExigirSessaoAttribute(string? perfilPermitido = null)
    {
        _perfilPermitido = perfilPermitido;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var sessao = context.HttpContext.Session;
        var perfilAtual = sessao.GetString("perfil");
        if (sessao.GetInt32("id") is null ||
            (perfilAtual != "Administrador" && perfilAtual != "Funcionario"))
        {
            sessao.Clear();
            context.Result = new RedirectToActionResult("Login", "Autenticacao", null);
            return Task.CompletedTask;
        }

        if (!string.IsNullOrWhiteSpace(_perfilPermitido) &&
            !string.Equals(perfilAtual, _perfilPermitido, StringComparison.Ordinal))
        {
            context.Result = perfilAtual == "Administrador"
                ? new RedirectToActionResult("Dashboard", "Administrador", null)
                : new RedirectToActionResult("RegistrarPedido", "Funcionario", null);
        }

        return Task.CompletedTask;
    }
}
