using FluentValidation;
using Microsoft.Extensions.Logging;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Llamadas;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Application.Features.Revision.Dtos;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Revision;

/// <summary>
/// POST /api/evaluaciones/{id}/confirmar y PUT /api/evaluaciones/{id}/ajuste.
/// Solo el Supervisor asignado a la campaña de la llamada. Cada decisión se ejecuta en UNA transacción
/// (revisión condicionada + Revision_humana + auditoría) y responde con el EvaluacionDto vigente.
/// </summary>
public sealed class RevisionEvaluaciones(
    IQualiaDbContext db,
    IUsuarioActual usuarioActual,
    ServicioRevision servicio,
    ConsultasLlamadas consultasLlamadas,
    IValidator<ConfirmarEvaluacionDto> validadorConfirmacion,
    IValidator<AjustarEvaluacionDto> validadorAjuste,
    ILogger<RevisionEvaluaciones> logger)
{
    public async Task<EvaluacionDto> ConfirmarAsync(Guid evaluacionId, ConfirmarEvaluacionDto dto, CancellationToken ct)
    {
        var supervisor = usuarioActual.RequerirRol(RolEmpresa.Supervisor);
        await validadorConfirmacion.ValidarOLanzarAsync(dto, ct);

        var contexto = await servicio.CargarAsync(evaluacionId, supervisor.UsuarioId, ct);

        await using (var transaccion = await db.BeginTransactionAsync(ct))
        {
            var revision = await servicio.ConfirmarAsync(contexto, supervisor.UsuarioId, dto.RevisionEsperada!.Value, ct);
            await transaccion.CommitAsync(ct);
            logger.LogInformation("Evaluación {EvaluacionId} confirmada por {SupervisorId} (revisión {Revision})",
                evaluacionId, supervisor.UsuarioId, revision);
        }

        return await LeerVigenteAsync(contexto.Llamada.Id, ct);
    }

    public async Task<EvaluacionDto> AjustarAsync(Guid evaluacionId, AjustarEvaluacionDto dto, CancellationToken ct)
    {
        var supervisor = usuarioActual.RequerirRol(RolEmpresa.Supervisor);
        await validadorAjuste.ValidarOLanzarAsync(dto, ct);

        var contexto = await servicio.CargarAsync(evaluacionId, supervisor.UsuarioId, ct);

        await using (var transaccion = await db.BeginTransactionAsync(ct))
        {
            var revision = await servicio.AjustarAsync(contexto, supervisor.UsuarioId, dto, ct);
            await transaccion.CommitAsync(ct);
            logger.LogInformation("Evaluación {EvaluacionId} ajustada por {SupervisorId} (revisión {Revision})",
                evaluacionId, supervisor.UsuarioId, revision);
        }

        return await LeerVigenteAsync(contexto.Llamada.Id, ct);
    }

    /// <summary>Lee el resultado ya confirmado, sin entidades rastreadas de esta unidad de trabajo.</summary>
    private async Task<EvaluacionDto> LeerVigenteAsync(Guid llamadaId, CancellationToken ct)
    {
        db.LimpiarSeguimiento();
        return await consultasLlamadas.ObtenerEvaluacionAsync(llamadaId, ct);
    }
}
