using FluentValidation;

namespace Qualia.Application.Common.Errores;

public static class ValidacionExtensiones
{
    /// Ejecuta el validador y, si hay errores, lanza 400 VALIDACION con el detalle por campo.
    public static async Task ValidarOLanzarAsync<T>(this IValidator<T> validador, T instancia, CancellationToken ct)
    {
        var resultado = await validador.ValidateAsync(instancia, ct);
        if (!resultado.IsValid)
        {
            throw ExcepcionNegocio.Validacion(resultado.ToDictionary());
        }
    }
}
