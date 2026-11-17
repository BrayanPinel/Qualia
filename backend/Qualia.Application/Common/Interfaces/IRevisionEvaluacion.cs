using Qualia.Application.Features.Revision.Dtos;

namespace Qualia.Application.Common.Interfaces;

/// <summary>
/// CONTRATO B → A (resolución de disputas con o sin cambio de notas).
/// Lo implementa el Rol B (<c>ServicioRevision</c>) y lo usa el Rol A en
/// PUT /api/disputas/{id}/resolucion, sin duplicar el cálculo ni el control de concurrencia.
///
/// Reglas para quien lo invoca:
/// <list type="number">
/// <item>Los métodos que escriben se ejecutan DENTRO de una transacción abierta por el llamador sobre el
/// mismo <c>IQualiaDbContext</c> del scope: hacen SaveChanges, pero NO Commit.</item>
/// <item>Comparan <c>revisionEsperada</c> con Evaluacion_IA.revision y la incrementan de forma atómica;
/// si no coincide lanzan 409 <c>REVISION_DESACTUALIZADA</c>.</item>
/// <item>Los errores de negocio se lanzan como <c>ExcepcionNegocio</c>.</item>
/// </list>
/// </summary>
public interface IRevisionEvaluacion
{
    /// <summary>404 si la evaluación no existe en la empresa; 403 si el supervisor no está asignado a su campaña.</summary>
    Task VerificarSupervisorAsignadoAsync(Guid evaluacionId, Guid supervisorId, CancellationToken cancellationToken);

    /// <summary>
    /// Ajuste completo (disputa "Modificar"): mismas validaciones que PUT /api/evaluaciones/{id}/ajuste.
    /// Conserva el resultado de IA y deja la evaluación AjustadaPorSupervisor. Devuelve la nueva revisión.
    /// </summary>
    Task<int> AjustarAsync(Guid evaluacionId, Guid supervisorId, AjustarEvaluacionDto ajuste, CancellationToken cancellationToken);

    /// <summary>
    /// Decisión sin cambio de notas (disputa "Mantener"): registra la revisión humana con su comentario
    /// e incrementa la revisión, sin alterar el estado ni los puntajes. Devuelve la nueva revisión.
    /// </summary>
    Task<int> RegistrarDecisionSinCambiosAsync(
        Guid evaluacionId,
        Guid supervisorId,
        int revisionEsperada,
        string comentarios,
        CancellationToken cancellationToken);
}
