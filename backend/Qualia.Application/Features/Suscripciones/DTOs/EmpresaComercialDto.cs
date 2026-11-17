namespace Qualia.Application.Features.Suscripciones.Comun;

/// <summary>EmpresaComercialDto (fase1 §7.7) para administración global por PlatformAdmin.</summary>
public sealed record EmpresaComercialDto(
    Guid EmpresaId,
    string Codigo,
    string Nombre,
    string Estado,
    SuscripcionDto? Suscripcion,
    ConsumoDto? Consumo);
