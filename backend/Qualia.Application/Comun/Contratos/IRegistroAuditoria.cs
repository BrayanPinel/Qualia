namespace Qualia.Application.Comun.Contratos;

/// <summary>
/// CONTRATO A → B, C, D (auditoría empresarial, tabla Auditoria).
/// Lo implementa el Rol A. Mientras tanto, <c>RegistroAuditoriaProvisional</c> solo escribe en el log.
///
/// La implementación agrega el registro al <c>IQualiaDbContext</c> del scope SIN guardar;
/// quien llama hace SaveChanges dentro de su propia unidad de trabajo.
/// Nunca incluir contraseñas, tokens ni secretos en <see cref="EntradaAuditoria.Antes"/>/<see cref="EntradaAuditoria.Despues"/>.
/// </summary>

public interface IRegistroAuditoria
{
    Task RegistrarAsync(EntradaAuditoria entrada, CancellationToken cancellationToken);
}

/// <param name="UsuarioId">Null cuando la acción la ejecuta el sistema.</param>
/// <param name="EntidadId">Referencia lógica al recurso indicado por <paramref name="Entidad"/>.</param>
public sealed record EntradaAuditoria(
    Guid EmpresaId,
    Guid? UsuarioId,
    string Accion,
    string Entidad,
    Guid? EntidadId,
    object? Antes = null,
    object? Despues = null);
