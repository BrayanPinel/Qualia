using Qualia.Domain.Enums;

namespace Qualia.Domain.Entidades;

/// Empresa cliente de la plataforma. Tabla global: es la raíz de aislamiento
/// de todas las tablas empresariales.
public class Empresa
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public EstadoEmpresa Estado { get; set; } = EstadoEmpresa.Activa;
    public DateTime FechaRegistro { get; set; }

    /// Mínimo de llamadas elegibles para calcular scorecards.
    public int MinimoMuestra { get; set; } = 5;
}
