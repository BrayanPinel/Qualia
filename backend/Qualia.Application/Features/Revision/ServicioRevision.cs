using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Features.Procesamiento;
using Qualia.Application.Features.Revision.Dtos;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;
using Qualia.Domain.Servicios;

namespace Qualia.Application.Features.Revision;

/// <summary>
/// Núcleo de la revisión humana. Lo usan los endpoints de confirmación/ajuste y,
/// mediante <see cref="IRevisionEvaluacion"/>, la resolución de disputas del Rol A.
/// Nunca modifica Detalle_evaluacion ni Evidencia_criterio: los resultados de IA se conservan (RN-12).
/// Control de concurrencia optimista sobre Evaluacion_IA.revision: UPDATE condicionado a
/// revision = revisionEsperada; si no afecta filas, 409 REVISION_DESACTUALIZADA.
/// Los métodos que escriben NO abren ni confirman transacciones: lo hace quien los invoca.
/// </summary>
public sealed class ServicioRevision(
    IQualiaDbContext db,
    IValidator<AjustarEvaluacionDto> validadorAjuste,
    IRegistroAuditoria auditoria,
    TimeProvider reloj) : IRevisionEvaluacion
{
    public const string ComentarioConfirmacion = "Confirmación: el supervisor declara haber revisado todos los criterios.";

    /// <summary>Datos necesarios para revisar una evaluación (solo lectura).</summary>
    internal sealed record ContextoRevision(EvaluacionIA Evaluacion, Llamada Llamada, IReadOnlyList<CriterioRubrica> Criterios);

    public async Task VerificarSupervisorAsignadoAsync(Guid evaluacionId, Guid supervisorId, CancellationToken cancellationToken) =>
        await CargarAsync(evaluacionId, supervisorId, cancellationToken);

    /// <summary>
    /// Carga la evaluación con sus detalles, la llamada y los criterios de la rúbrica FIJADA en la llamada.
    /// 404 si no existe en la empresa (el filtro global oculta las de otras empresas);
    /// 403 si el supervisor no está asignado a la campaña de la llamada.
    /// </summary>
    internal async Task<ContextoRevision> CargarAsync(Guid evaluacionId, Guid supervisorId, CancellationToken ct)
    {
        var evaluacion = await db.EvaluacionesIA.AsNoTracking()
            .Include(e => e.Detalles).ThenInclude(d => d.Evidencias)
            .FirstOrDefaultAsync(e => e.Id == evaluacionId, ct)
            ?? throw ExcepcionNegocio.NoEncontrado("EVALUACION_NO_ENCONTRADA", "La evaluación no existe.");

        var llamada = await db.Llamadas.AsNoTracking()
            .Include(l => l.Rubrica).ThenInclude(r => r!.Criterios)
            .FirstAsync(l => l.Id == evaluacion.LlamadaId, ct);

        var asignado = await db.UsuariosCampana.AnyAsync(
            uc => uc.UsuarioId == supervisorId && uc.CampanaId == llamada.Rubrica!.CampanaId, ct);
        if (!asignado)
        {
            throw ExcepcionNegocio.Prohibido("CAMPANA_NO_ASIGNADA", "No está asignado a la campaña de esta evaluación.");
        }

        return new ContextoRevision(evaluacion, llamada, [.. llamada.Rubrica!.Criterios]);
    }

    /// <summary>
    /// Confirmación (POST /api/evaluaciones/{id}/confirmar). Exige evaluación completa y sin evidencias
    /// inválidas pendientes; la evidencia inválida se corrige con un ajuste completo.
    /// Confirmar una evaluación marcada por otra falla de la IA (falta sin fragmento o nota dudosa)
    /// la oficializa y quita la marca de revisión.
    /// Una confirmación posterior a un ajuste NO borra el ajuste: el puntaje vigente sigue siendo el humano.
    /// </summary>
    internal async Task<int> ConfirmarAsync(ContextoRevision contexto, Guid supervisorId, int revisionEsperada, CancellationToken ct)
    {
        var evaluacion = contexto.Evaluacion;
        AsegurarRevisionVigente(evaluacion, revisionEsperada);

        if (evaluacion.Estado == EstadoEvaluacion.ConfirmadaPorSupervisor)
        {
            throw ExcepcionNegocio.Conflicto("EVALUACION_YA_CONFIRMADA", "La evaluación ya fue confirmada.");
        }

        var criteriosEvaluados = evaluacion.Detalles.Select(d => d.CriterioId).ToHashSet();
        if (!criteriosEvaluados.SetEquals(contexto.Criterios.Select(c => c.Id)))
        {
            throw ExcepcionNegocio.Conflicto("EVALUACION_INCOMPLETA",
                "La evaluación no contiene exactamente los criterios de la rúbrica de la llamada.");
        }

        if (evaluacion.Estado == EstadoEvaluacion.EvaluadaPorIA)
        {
            await AsegurarEvidenciaIAValidaAsync(contexto, ct);
        }

        var nuevaRevision = await IncrementarRevisionAsync(
            evaluacion.Id, revisionEsperada, EstadoEvaluacion.ConfirmadaPorSupervisor, ct, limpiarRequiereRevision: true);

        db.RevisionesHumanas.Add(new RevisionHumana
        {
            EmpresaId = evaluacion.EmpresaId,
            EvaluacionId = evaluacion.Id,
            SupervisorId = supervisorId,
            FechaRevision = reloj.GetUtcNow().UtcDateTime,
            Comentarios = ComentarioConfirmacion,
        });

        await auditoria.RegistrarAsync(evaluacion.EmpresaId, supervisorId, "ConfirmarEvaluacion", "EvaluacionIA", evaluacion.Id,
            new { Revision = revisionEsperada, Estado = evaluacion.Estado.ToString() },
            new { Revision = nuevaRevision, Estado = EstadoEvaluacion.ConfirmadaPorSupervisor.ToString() }, ct);

        await db.SaveChangesAsync(ct);
        return nuevaRevision;
    }

    public async Task<int> AjustarAsync(Guid evaluacionId, Guid supervisorId, AjustarEvaluacionDto ajuste, CancellationToken cancellationToken)
    {
        await validadorAjuste.ValidarOLanzarAsync(ajuste, cancellationToken);
        var contexto = await CargarAsync(evaluacionId, supervisorId, cancellationToken);
        return await AjustarAsync(contexto, supervisorId, ajuste, cancellationToken);
    }

    /// <summary>
    /// Ajuste completo (PUT /api/evaluaciones/{id}/ajuste): todos los criterios de la rúbrica exactamente una vez,
    /// puntajes 0–100, justificación obligatoria y evidencias que existan en segmentos de LA MISMA llamada (RN-09).
    /// Guarda Revision_humana + Criterio_revisado + Evidencia_revision; el total se recalcula en el servidor (RN-08).
    /// </summary>
    internal async Task<int> AjustarAsync(ContextoRevision contexto, Guid supervisorId, AjustarEvaluacionDto ajuste, CancellationToken ct)
    {
        var evaluacion = contexto.Evaluacion;
        var revisionEsperada = ajuste.RevisionEsperada!.Value;
        AsegurarRevisionVigente(evaluacion, revisionEsperada);

        ValidarCobertura(contexto, ajuste);
        await ValidarEvidenciasAsync(contexto, ajuste, ct);

        var detallePorCriterio = evaluacion.Detalles.ToDictionary(d => d.CriterioId);
        var criterioPorId = contexto.Criterios.ToDictionary(c => c.Id);

        var nuevaRevision = await IncrementarRevisionAsync(
            evaluacion.Id, revisionEsperada, EstadoEvaluacion.AjustadaPorSupervisor, ct, limpiarRequiereRevision: true);

        var revision = new RevisionHumana
        {
            EmpresaId = evaluacion.EmpresaId,
            EvaluacionId = evaluacion.Id,
            SupervisorId = supervisorId,
            FechaRevision = reloj.GetUtcNow().UtcDateTime,
            Comentarios = ajuste.Motivo!.Trim(),
        };

        foreach (var entrada in ajuste.Criterios!)
        {
            var criterioRevisado = new CriterioRevisado
            {
                EmpresaId = evaluacion.EmpresaId,
                RevisionId = revision.Id,
                DetalleId = detallePorCriterio[entrada.CriterioId!.Value].Id,
                PuntajeAjustado = Math.Round(entrada.Puntaje!.Value, 2, MidpointRounding.AwayFromZero),
                Observacion = entrada.Justificacion!.Trim(),
            };

            foreach (var evidencia in entrada.Evidencias!)
            {
                criterioRevisado.Evidencias.Add(new EvidenciaRevision
                {
                    EmpresaId = evaluacion.EmpresaId,
                    CriterioRevisadoId = criterioRevisado.Id,
                    SegmentoId = evidencia.SegmentoId!.Value,
                    CitaTextual = evidencia.CitaTextual!.Trim(),
                });
            }

            revision.Criterios.Add(criterioRevisado);
        }

        db.RevisionesHumanas.Add(revision);

        var totalIA = CalculadoraPuntaje.Total(evaluacion.Detalles.Select(d => (d.PuntajeCriterio, criterioPorId[d.CriterioId].Peso)));
        var totalHumano = CalculadoraPuntaje.Total(ajuste.Criterios!.Select(c =>
            (Math.Round(c.Puntaje!.Value, 2, MidpointRounding.AwayFromZero), criterioPorId[c.CriterioId!.Value].Peso)));

        await auditoria.RegistrarAsync(evaluacion.EmpresaId, supervisorId, "AjustarEvaluacion", "EvaluacionIA", evaluacion.Id,
            new { Revision = revisionEsperada, Estado = evaluacion.Estado.ToString(), PuntajeTotalIA = totalIA },
            new
            {
                Revision = nuevaRevision,
                Estado = EstadoEvaluacion.AjustadaPorSupervisor.ToString(),
                PuntajeTotal = totalHumano,
                Motivo = revision.Comentarios,
                RevisionHumanaId = revision.Id,
            },
            ct);

        await db.SaveChangesAsync(ct);
        return nuevaRevision;
    }

    public async Task<int> RegistrarDecisionSinCambiosAsync(
        Guid evaluacionId,
        Guid supervisorId,
        int revisionEsperada,
        string comentarios,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(comentarios))
        {
            throw ExcepcionNegocio.Validacion(new Dictionary<string, string[]> { ["comentarios"] = ["El comentario es obligatorio."] });
        }

        var contexto = await CargarAsync(evaluacionId, supervisorId, cancellationToken);
        var evaluacion = contexto.Evaluacion;
        AsegurarRevisionVigente(evaluacion, revisionEsperada);

        var nuevaRevision = await IncrementarRevisionAsync(evaluacion.Id, revisionEsperada, nuevoEstado: null, cancellationToken);

        db.RevisionesHumanas.Add(new RevisionHumana
        {
            EmpresaId = evaluacion.EmpresaId,
            EvaluacionId = evaluacion.Id,
            SupervisorId = supervisorId,
            FechaRevision = reloj.GetUtcNow().UtcDateTime,
            Comentarios = comentarios.Trim(),
        });

        await auditoria.RegistrarAsync(evaluacion.EmpresaId, supervisorId, "RevisarEvaluacionSinCambios", "EvaluacionIA", evaluacion.Id,
            new { Revision = revisionEsperada }, new { Revision = nuevaRevision }, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return nuevaRevision;
    }

    /// <summary>Comprobación temprana con mensaje claro; la garantía real es el UPDATE condicionado.</summary>
    private static void AsegurarRevisionVigente(EvaluacionIA evaluacion, int revisionEsperada)
    {
        if (evaluacion.Revision != revisionEsperada)
        {
            throw RevisionDesactualizada();
        }
    }

    private async Task<int> IncrementarRevisionAsync(
        Guid evaluacionId,
        int revisionEsperada,
        EstadoEvaluacion? nuevoEstado,
        CancellationToken ct,
        bool limpiarRequiereRevision = false)
    {
        // UPDATE evaluacion_ia SET revision = revision + 1 [, estado = ...] WHERE id = @id AND revision = @esperada.
        // PostgreSQL bloquea la fila: de dos supervisores con la misma revisionEsperada, el segundo no afecta filas.
        var consulta = db.EvaluacionesIA.Where(e => e.Id == evaluacionId && e.Revision == revisionEsperada);

        int filas;
        if (nuevoEstado is { } estado)
        {
            filas = limpiarRequiereRevision
                ? await consulta.ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.Revision, e => e.Revision + 1)
                    .SetProperty(e => e.Estado, estado)
                    .SetProperty(e => e.RequiereRevision, false), ct)
                : await consulta.ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.Revision, e => e.Revision + 1)
                    .SetProperty(e => e.Estado, estado), ct);
        }
        else
        {
            filas = await consulta.ExecuteUpdateAsync(s => s.SetProperty(e => e.Revision, e => e.Revision + 1), ct);
        }

        if (filas == 0)
        {
            throw RevisionDesactualizada();
        }

        return revisionEsperada + 1;
    }

    private static ExcepcionNegocio RevisionDesactualizada() => ExcepcionNegocio.Conflicto(
        "REVISION_DESACTUALIZADA",
        "La evaluación fue modificada por otra revisión. Vuelva a consultarla antes de decidir.");

    /// <summary>
    /// RN-10: una evaluación con evidencia inválida descartada exige un ajuste antes de oficializarse.
    /// Además se comprueba que cada evidencia de IA apunte a un segmento de la misma llamada.
    /// </summary>
    private async Task AsegurarEvidenciaIAValidaAsync(ContextoRevision contexto, CancellationToken ct)
    {
        if (contexto.Evaluacion.RequiereRevision
            && contexto.Evaluacion.Detalles.Any(d => d.Observacion.Contains(ConstructorEvaluacion.MarcaEvidenciaDescartada)))
        {
            throw ExcepcionNegocio.Conflicto("EVIDENCIA_INVALIDA",
                "La evaluación tiene evidencia inválida. Corríjala mediante un ajuste completo antes de oficializarla.");
        }

        var segmentosEvidencia = contexto.Evaluacion.Detalles.SelectMany(d => d.Evidencias).Select(e => e.SegmentoId).Distinct().ToList();
        if (segmentosEvidencia.Count == 0)
        {
            return;
        }

        var validos = await db.SegmentosTranscripcion
            .CountAsync(s => s.LlamadaId == contexto.Llamada.Id && segmentosEvidencia.Contains(s.Id), ct);
        if (validos != segmentosEvidencia.Count)
        {
            throw ExcepcionNegocio.Conflicto("EVIDENCIA_INVALIDA",
                "La evaluación tiene evidencia que no pertenece a la llamada. Corríjala mediante un ajuste completo.");
        }
    }

    /// <summary>El ajuste incluye todos los criterios de la rúbrica de la llamada, exactamente una vez.</summary>
    private static void ValidarCobertura(ContextoRevision contexto, AjustarEvaluacionDto ajuste)
    {
        var esperados = contexto.Criterios.Select(c => c.Id).ToHashSet();
        var recibidos = ajuste.Criterios!.Select(c => c.CriterioId!.Value).ToList();

        var desconocidos = recibidos.Where(id => !esperados.Contains(id)).Distinct().ToList();
        var faltantes = esperados.Where(id => !recibidos.Contains(id)).ToList();

        if (desconocidos.Count == 0 && faltantes.Count == 0 && recibidos.Count == esperados.Count)
        {
            return;
        }

        var errores = new Dictionary<string, string[]>();
        if (faltantes.Count > 0)
        {
            var nombres = contexto.Criterios.Where(c => faltantes.Contains(c.Id)).Select(c => c.Nombre);
            errores["criterios"] = [$"Faltan criterios de la rúbrica: {string.Join(", ", nombres)}."];
        }

        if (desconocidos.Count > 0)
        {
            errores["criterioId"] = [$"Criterios que no pertenecen a la rúbrica de la llamada: {string.Join(", ", desconocidos)}."];
        }

        throw new ExcepcionNegocio(400, "AJUSTE_INCOMPLETO",
            "El ajuste debe incluir cada criterio de la rúbrica exactamente una vez.", errores);
    }

    /// <summary>RN-09: cada cita debe existir en un segmento de la misma llamada (misma tolerancia que la evidencia de IA).</summary>
    private async Task ValidarEvidenciasAsync(ContextoRevision contexto, AjustarEvaluacionDto ajuste, CancellationToken ct)
    {
        var segmentos = await db.SegmentosTranscripcion.AsNoTracking()
            .Where(s => s.LlamadaId == contexto.Llamada.Id)
            .ToDictionaryAsync(s => s.Id, s => s.Texto, ct);

        var errores = new Dictionary<string, string[]>();
        for (var i = 0; i < ajuste.Criterios!.Count; i++)
        {
            var evidencias = ajuste.Criterios[i].Evidencias!;
            for (var j = 0; j < evidencias.Count; j++)
            {
                var evidencia = evidencias[j];
                var clave = $"criterios[{i}].evidencias[{j}]";

                if (!segmentos.TryGetValue(evidencia.SegmentoId!.Value, out var texto))
                {
                    errores[clave] = ["El segmento no pertenece a la transcripción de esta llamada."];
                }
                else if (!ConstructorEvaluacion.EsCitaValida(evidencia.CitaTextual!.Trim(), texto))
                {
                    errores[clave] = ["La cita no aparece en el texto del segmento indicado."];
                }
            }
        }

        if (errores.Count > 0)
        {
            throw new ExcepcionNegocio(400, "EVIDENCIA_INVALIDA", "Una o más evidencias no son válidas.", errores);
        }
    }
}
