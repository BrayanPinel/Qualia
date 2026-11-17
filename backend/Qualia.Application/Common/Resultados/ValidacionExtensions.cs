using FluentValidation;
using FluentValidation.Results;

namespace Qualia.Application.Common.Resultados;

public static class ValidacionExtensions
{
    /// <summary>
    /// Ejecuta el validador de FluentValidation y devuelve null si es válido,
    /// o el <see cref="ErrorAplicacion"/> uniforme 400 si hay fallos.
    /// </summary>
    public static async Task<ErrorAplicacion?> ValidarAsync<T>(
        this IValidator<T> validador,
        T instancia,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validador);
        ArgumentNullException.ThrowIfNull(instancia);

        var resultado = await validador.ValidateAsync(instancia, cancellationToken);
        return resultado.IsValid ? null : resultado.AErrorAplicacion();
    }

    /// <summary>
    /// Convierte un resultado inválido de FluentValidation en el error uniforme 400,
    /// con claves en camelCase para coincidir con el JSON del contrato.
    /// </summary>
    public static ErrorAplicacion AErrorAplicacion(this ValidationResult resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        if (resultado.IsValid)
        {
            throw new InvalidOperationException("No se puede convertir una validación exitosa en ErrorAplicacion.");
        }

        var errores = resultado.Errors
            .GroupBy(e => ACamelCaseRuta(e.PropertyName))
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        return ErrorAplicacion.Validacion(errores);
    }

    private static string ACamelCaseRuta(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return "general";
        }

        var segmentos = nombre.Split('.');
        for (var i = 0; i < segmentos.Length; i++)
        {
            segmentos[i] = ACamelCaseSegmento(segmentos[i]);
        }

        return string.Join('.', segmentos);
    }

    private static string ACamelCaseSegmento(string segmento)
    {
        if (string.IsNullOrEmpty(segmento) || char.IsLower(segmento[0]))
        {
            return segmento;
        }

        return string.Concat(char.ToLowerInvariant(segmento[0]).ToString(), segmento.AsSpan(1));
    }
}
