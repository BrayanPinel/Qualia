using Qualia.Domain.Common;

namespace Qualia.Domain.Entidades;

/// Auditoría empresarial. UsuarioId es null cuando la acción la ejecuta el sistema.
/// Antes y Despues se guardan como jsonb y nunca deben contener
/// contraseñas, hashes, tokens ni secretos.
public class Auditoria : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid? UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;

    /// Referencia lógica al recurso indicado por Entidad.
    public Guid? EntidadId { get; set; }

    public string? Antes { get; set; }
    public string? Despues { get; set; }
    public DateTime Fecha { get; set; }
}
