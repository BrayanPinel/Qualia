using Qualia.Application.Common.Errores;

namespace Qualia.Application.Common.Resultados;

/// Puente entre los dos estilos de manejo de errores del proyecto:
/// los módulos que devuelven Resultado (suscripciones) y los que lanzan ExcepcionNegocio
/// (llamadas, campañas, revisión). El middleware global traduce la excepción al mismo ErrorDto.
public static class ResultadoExcepciones
{
    public static ExcepcionNegocio AExcepcion(this ErrorAplicacion error) => new(
        (int)error.Tipo,
        error.Codigo,
        error.Mensaje,
        error.Errores is null ? null : new Dictionary<string, string[]>(error.Errores));

    /// Lanza ExcepcionNegocio si el resultado es un fallo.
    public static void LanzarSiFallo(this Resultado resultado)
    {
        if (!resultado.EsExitoso)
        {
            throw resultado.Error.AExcepcion();
        }
    }

    /// Devuelve el valor o lanza ExcepcionNegocio si el resultado es un fallo.
    public static T ValorOLanzar<T>(this Resultado<T> resultado) =>
        resultado.EsExitoso ? resultado.Valor : throw resultado.Error.AExcepcion();
}
