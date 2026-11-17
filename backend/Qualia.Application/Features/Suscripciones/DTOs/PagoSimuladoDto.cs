using System.Text.Json.Serialization;

namespace Qualia.Application.Features.Suscripciones.PagoSimulado;

/// <summary>PagoSimuladoDto: resultado "Aprobado" | "Rechazado".</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PagoSimuladoDto(string Resultado);
