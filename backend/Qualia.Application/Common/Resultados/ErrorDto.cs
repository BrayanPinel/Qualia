namespace Qualia.Application.Common.Resultados;

/// <summary>
/// Contrato uniforme de respuesta de error HTTP de Qualia 
/// </summary>
public sealed record ErrorDto(
    int Status,
    string Codigo,
    string Mensaje,
    IReadOnlyDictionary<string, string[]>? Errores,
    string TraceId);
