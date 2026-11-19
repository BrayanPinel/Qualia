using System.Text.Json.Serialization;

namespace Qualia.Application.Features.Suscripciones.Plataforma.CambiarSuscripcion;

/// CambiarSuscripcionDto.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CambiarSuscripcionDto(
    Guid PlanId,
    string Estado,
    DateTime FechaVencimiento,
    bool PermiteExcedente,
    string Motivo);

public sealed record CambiarSuscripcionCommand(
    Guid EmpresaId,
    Guid AdministradorId,
    CambiarSuscripcionDto Datos);
