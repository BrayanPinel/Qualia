using Qualia.Application.Features.Revision.Dtos;

namespace Qualia.Application.Common.Interfaces;

/// CONTRATO B → A (resolución de disputas con o sin cambio de notas).
/// Lo implementa el Rol B (ServicioRevision) y lo usa el Rol A en
/// PUT /api/disputas/{id}/resolucion, sin duplicar el cálculo ni el control de concurrencia.
///
/// Reglas para quien lo invoca:
/// Los métodos que escriben se ejecutan DENTRO de una transacción abierta por el llamador sobre el
/// mismo IQualiaDbContext del scope: hacen SaveChanges, pero NO Commit.
/// Comparan revisionEsperada con Evaluacion_IA.revision y la incrementan de forma atómica;
/// si no coincide lanzan 409 REVISION_DESACTUALIZADA.
/// Los errores de negocio se lanzan como ExcepcionNegocio.
public interface IRevisionEvaluacion
{
    /// 404 si la evaluación no existe en la empresa; 403 si el supervisor no está asignado a su campaña.
    Task VerificarSupervisorAsignadoAsync(Guid evaluacionId, Guid supervisorId, CancellationToken cancellationToken);

    /// Ajuste completo (disputa "Modificar"): mismas validaciones que PUT /api/evaluaciones/{id}/ajuste.
    /// Conserva el resultado de IA y deja la evaluación AjustadaPorSupervisor. Devuelve la nueva revisión.
    Task<int> AjustarAsync(Guid evaluacionId, Guid supervisorId, AjustarEvaluacionDto ajuste, CancellationToken cancellationToken);

    /// Decisión sin cambio de notas (disputa "Mantener"): registra la revisión humana con su comentario
    /// e incrementa la revisión, sin alterar el estado ni los puntajes. Devuelve la nueva revisión.
    Task<int> RegistrarDecisionSinCambiosAsync(
        Guid evaluacionId,
        Guid supervisorId,
        int revisionEsperada,
        string comentarios,
        CancellationToken cancellationToken);
}
