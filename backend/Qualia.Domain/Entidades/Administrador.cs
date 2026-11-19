namespace Qualia.Domain.Entidades;

/// Administrador global de la plataforma. Tabla global independiente de Usuario:
/// su identidad no pertenece a ninguna empresa y su correo es único en toda la plataforma.
public class Administrador
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    /// Hash de contraseña; nunca se expone en DTOs ni en auditoría.
    public string PasswordHash { get; set; } = string.Empty;
}
