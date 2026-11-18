using Qualia.Application.Features.Suscripciones.Comun;

namespace Qualia.Application.Features.Empresas.Dtos;

// Las propiedades de entrada son anulables para que un campo ausente se informe como error de validación
// en lugar de tomar un valor por defecto silencioso.

/// <summary>RegistrarEmpresaDto { codigoEmpresa, nombreEmpresa, planId, nombreAdministrador, email, password }.</summary>
public sealed class RegistrarEmpresaDto
{
    public string? CodigoEmpresa { get; set; }
    public string? NombreEmpresa { get; set; }
    public Guid? PlanId { get; set; }
    public string? NombreAdministrador { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

/// <summary>
/// Respuesta de POST /api/empresas. <see cref="UsuarioAdministradorId"/> es el Usuario AdminEmpresa
/// de la empresa, no un Administrador global de la plataforma.
/// </summary>
public sealed record EmpresaRegistradaDto(
    Guid EmpresaId,
    string CodigoEmpresa,
    Guid UsuarioAdministradorId,
    SuscripcionDto Suscripcion);
