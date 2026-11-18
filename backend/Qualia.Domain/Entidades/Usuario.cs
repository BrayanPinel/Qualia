using Qualia.Domain.Common;
using Qualia.Domain.Enums;

namespace Qualia.Domain.Entidades;

/// <summary>Usuario de una empresa. El correo es único dentro de la empresa, no global.</summary>
public class Usuario : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public RolEmpresa Rol { get; set; }
    public bool Activo { get; set; } = true;

    /// <summary>Hash de contraseña; nunca se expone en DTOs ni en auditoría.</summary>
    public string PasswordHash { get; set; } = string.Empty;
}
