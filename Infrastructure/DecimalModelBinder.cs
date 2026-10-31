using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SIVAD.Infrastructure;

/// <summary>
/// Aceita valores monetários digitados no padrão brasileiro ou internacional.
/// Exemplos: 100,34; 100.34; 1.234,56; 1,234.56.
/// O valor normalizado chega ao model como decimal antes de ser enviado ao SQL Server.
/// </summary>
public sealed class DecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var resultado = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (resultado == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, resultado);
        var texto = resultado.FirstValue?.Trim();

        if (string.IsNullOrWhiteSpace(texto))
            return Task.CompletedTask;

        if (TryParseDecimal(texto, out var valor))
        {
            bindingContext.Result = ModelBindingResult.Success(valor);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.AddModelError(
            bindingContext.ModelName,
            "Informe um valor monetário válido, por exemplo 100,34.");
        return Task.CompletedTask;
    }

    private static bool TryParseDecimal(string texto, out decimal valor)
    {
        // Remove espaços comuns e espaços inseparáveis copiados de planilhas.
        var normalizado = texto.Replace(" ", string.Empty).Replace("\u00A0", string.Empty);
        var ultimaVirgula = normalizado.LastIndexOf(',');
        var ultimoPonto = normalizado.LastIndexOf('.');

        if (ultimaVirgula >= 0 && ultimoPonto >= 0)
        {
            // O separador que aparece por último é o decimal; o outro é milhar.
            var separadorDecimal = Math.Max(ultimaVirgula, ultimoPonto);
            var separadorMilhar = separadorDecimal == ultimaVirgula ? '.' : ',';
            normalizado = normalizado.Replace(separadorMilhar.ToString(), string.Empty);
            normalizado = normalizado.Replace(separadorDecimal == ultimaVirgula ? ',' : '.', '.');
        }
        else if (ultimaVirgula >= 0)
        {
            normalizado = normalizado.Replace(',', '.');
        }
        // Quando há somente ponto, ele é tratado como separador decimal.

        return decimal.TryParse(
            normalizado,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out valor);
    }
}

public sealed class DecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Metadata.UnderlyingOrModelType == typeof(decimal)
            ? new DecimalModelBinder()
            : null;
    }
}
