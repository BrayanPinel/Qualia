namespace Qualia.Application.Common.Resultados;

/// <summary>
/// Categoría del error de aplicación. El valor numérico es el código HTTP
/// con el que la API lo expone (fase1 §7.10: 400, 401, 403, 404, 409).
/// </summary>
public enum TipoError
{
    Validacion = 400,
    NoAutenticado = 401,
    Prohibido = 403,
    NoEncontrado = 404,
    Conflicto = 409,
}

/// <summary>
/// Error de negocio o validación, independiente de HTTP. La API lo traduce a
/// <c>ErrorDto { status, codigo, mensaje, errores?, traceId }</c> (fase1 §7.7 y §7.10).
/// </summary>
public sealed record ErrorAplicacion(
    TipoError Tipo,
    string Codigo,
    string Mensaje,
    IReadOnlyDictionary<string, string[]>? Errores = null)
{
    public const string CodigoValidacion = "VALIDACION_FALLIDA";

    public static ErrorAplicacion Validacion(
        IReadOnlyDictionary<string, string[]> errores,
        string mensaje = "La solicitud contiene datos inválidos.") =>
        new(TipoError.Validacion, CodigoValidacion, mensaje, errores);

    public static ErrorAplicacion NoAutenticado(string codigo, string mensaje) =>
        new(TipoError.NoAutenticado, codigo, mensaje);

    public static ErrorAplicacion Prohibido(string codigo, string mensaje) =>
        new(TipoError.Prohibido, codigo, mensaje);

    public static ErrorAplicacion NoEncontrado(string codigo, string mensaje) =>
        new(TipoError.NoEncontrado, codigo, mensaje);

    public static ErrorAplicacion Conflicto(string codigo, string mensaje) =>
        new(TipoError.Conflicto, codigo, mensaje);

    public ErrorDto AErrorDto(string traceId) =>
        new((int)Tipo, Codigo, Mensaje, Errores, traceId);
}
