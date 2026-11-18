using Qualia.Domain.Common;

namespace Qualia.Domain.Entidades;

/// <summary>
/// Auditoría empresarial. <see cref="UsuarioId"/> es null cuando la acción la ejecuta el sistema.
/// <see cref="Antes"/> y <see cref="Despues"/> se guardan como jsonb y nunca deben contener
/// contraseñas, hashes, tokens ni secretos.
/// </summary>
public class Auditoria : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid? UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;

    /// <summary>Referencia lógica al recurso indicado por <see cref="Entidad"/>.</summary>
    public Guid? EntidadId { get; set; }

    public string? Antes { get; set; }
    public string? Despues { get; set; }
    public DateTime Fecha { get; set; }
}
