using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Usuarios.Dtos;

// La salida reutiliza UsuarioDto (Features/Campanas/Dtos): un solo DTO de usuario en toda la API.
// Entrada con propiedades anulables: un campo ausente se informa como error de validación.
// La empresa nunca viaja en el cuerpo: se toma de la sesión.

/// <summary>CrearUsuarioDto { nombre, email, password, rol }. "PlatformAdmin" no es un RolEmpresa: se rechaza.</summary>
public sealed class CrearUsuarioDto
{
    public string? Nombre { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public RolEmpresa? Rol { get; set; }
}

/// <summary>ActualizarUsuarioDto { nombre, activo }.</summary>
public sealed class ActualizarUsuarioDto
{
    public string? Nombre { get; set; }
    public bool? Activo { get; set; }
}
