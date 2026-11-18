namespace Qualia.Domain.Entidades;

/// <summary>
/// Administrador global de la plataforma. Tabla global independiente de <see cref="Usuario"/>:
/// su identidad no pertenece a ninguna empresa y su correo es único en toda la plataforma.
/// </summary>
public class Administrador
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    /// <summary>Hash de contraseña; nunca se expone en DTOs ni en auditoría.</summary>
    public string PasswordHash { get; set; } = string.Empty;
}
