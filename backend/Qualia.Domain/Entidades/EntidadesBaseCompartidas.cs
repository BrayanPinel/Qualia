using Qualia.Domain.Common;
using Qualia.Domain.Enums;

namespace Qualia.Domain.Entidades;

/// <summary>Tabla global empresa</summary>
public class Empresa
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public EstadoEmpresa Estado { get; set; } = EstadoEmpresa.Activa;
    public DateTime FechaRegistro { get; set; }
    public int MinimoMuestra { get; set; } = 5;
}

/// <summary>Tabla usuario.</summary>
public class Usuario : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public RolEmpresa Rol { get; set; }
    public bool Activo { get; set; } = true;
    public string PasswordHash { get; set; } = string.Empty;
}
