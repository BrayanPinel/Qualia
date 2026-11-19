namespace Qualia.Application.Features.Suscripciones.Comun;

/// EmpresaComercialDto para administración global por PlatformAdmin.
public sealed record EmpresaComercialDto(
    Guid EmpresaId,
    string Codigo,
    string Nombre,
    string Estado,
    SuscripcionDto? Suscripcion,
    ConsumoDto? Consumo);
