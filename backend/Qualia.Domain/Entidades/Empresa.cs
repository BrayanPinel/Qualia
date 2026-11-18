using Qualia.Domain.Enums;

namespace Qualia.Domain.Entidades;

/// <summary>
/// Empresa cliente de la plataforma. Tabla global: es la raíz de aislamiento
/// de todas las tablas empresariales.
/// </summary>
public class Empresa
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public EstadoEmpresa Estado { get; set; } = EstadoEmpresa.Activa;
    public DateTime FechaRegistro { get; set; }

    /// <summary>Mínimo de llamadas elegibles para calcular scorecards.</summary>
    public int MinimoMuestra { get; set; } = 5;
}
