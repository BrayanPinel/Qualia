using System.Text.Json.Serialization;

namespace Qualia.Application.Features.Suscripciones.PagoSimulado;

/// PagoSimuladoDto: resultado "Aprobado" | "Rechazado".
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PagoSimuladoDto(string Resultado);
