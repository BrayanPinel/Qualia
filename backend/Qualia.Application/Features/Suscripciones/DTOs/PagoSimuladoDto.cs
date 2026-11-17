using System.Text.Json.Serialization;

namespace Qualia.Application.Features.Suscripciones.PagoSimulado;

/// <summary>PagoSimuladoDto (fase1 §7.6): resultado "Aprobado" | "Rechazado".</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PagoSimuladoDto(string Resultado);
