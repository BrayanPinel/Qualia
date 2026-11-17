namespace Qualia.Application.Common.Errores;

/// <summary>
/// Error de negocio con código HTTP y código identificable.
/// El middleware global lo transforma en <see cref="ErrorDto"/>.
/// </summary>
public class ExcepcionNegocio(int status, string codigo, string mensaje, IDictionary<string, string[]>? errores = null)
    : Exception(mensaje)
{
    public int Status { get; } = status;
    public string Codigo { get; } = codigo;
    public IDictionary<string, string[]>? Errores { get; } = errores;

    public static ExcepcionNegocio Validacion(IDictionary<string, string[]> errores) =>
        new(400, "VALIDACION", "Uno o más campos no son válidos.", errores);

    public static ExcepcionNegocio Prohibido(string codigo, string mensaje) => new(403, codigo, mensaje);

    public static ExcepcionNegocio NoEncontrado(string codigo, string mensaje) => new(404, codigo, mensaje);

    public static ExcepcionNegocio Conflicto(string codigo, string mensaje) => new(409, codigo, mensaje);
}

/// <summary>Formato uniforme de error</summary>
public sealed record ErrorDto(
    int Status,
    string Codigo,
    string Mensaje,
    IDictionary<string, string[]>? Errores,
    string TraceId);
