using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Campanas.Dtos;

// ---------------------------- Entrada ----------------------------
// Las propiedades son anulables para que un campo ausente se informe como error de validación
// en lugar de tomar un valor por defecto silencioso.

/// <summary>GuardarCampanaDto { nombre, activa }.</summary>
public sealed class GuardarCampanaDto
{
    public string? Nombre { get; set; }
    public bool? Activa { get; set; }
}

/// <summary>CrearRubricaDto { nombre, criterios[] } — también se usa en PUT /api/rubricas/{id}.</summary>
public sealed class CrearRubricaDto
{
    public string? Nombre { get; set; }
    public List<CriterioEntradaDto>? Criterios { get; set; }
}

public sealed class CriterioEntradaDto
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public decimal? Peso { get; set; }
    public bool? EsCritico { get; set; }
    public decimal? UmbralCumplimiento { get; set; }
}

// ---------------------------- Salida ----------------------------

public sealed record CampanaDto(Guid Id, Guid EmpresaId, string Nombre, bool Activa, Guid? RubricaActivaId);

/// <summary>Usuario asignado a una campaña. Nunca incluye el hash de la contraseña.</summary>
public sealed record UsuarioDto(Guid Id, Guid EmpresaId, string Nombre, string Email, RolEmpresa Rol, bool Activo);

public sealed record RubricaDto(
    Guid Id,
    Guid EmpresaId,
    Guid CampanaId,
    string Nombre,
    int Version,
    EstadoRubrica Estado,
    IReadOnlyList<CriterioDto> Criterios);

public sealed record CriterioDto(
    Guid Id,
    string Nombre,
    string Descripcion,
    decimal Peso,
    bool EsCritico,
    decimal UmbralCumplimiento);
