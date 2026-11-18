using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Models;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Disputas.Dtos;
using Qualia.Application.Features.Revision.Dtos;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;
using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Disputas;

/// <summary>
/// Disputas (HU-05): el agente disputa una evaluación oficial propia si su plan lo permite; el supervisor
/// asignado a la campaña la resuelve. "Modificar" y "Mantener" delegan en <see cref="IRevisionEvaluacion"/>
/// (revisión humana), que conserva el resultado de IA, audita y controla la concurrencia con revisionEsperada.
/// </summary>
public sealed class GestionDisputas(
    IQualiaDbContext db,
    IUsuarioActual usuarioActual,
    IValidator<CrearDisputaDto> validadorCrear,
    IValidator<ResolverDisputaDto> validadorResolver,
    IValidator<FiltroDisputasDto> validadorFiltro,
    IConsultaPlanService plan,
    IRevisionEvaluacion revision,
    IRegistroAuditoria auditoria,
    TimeProvider reloj)
{
    public const string CodigoEvaluacionNoEncontrada = "EVALUACION_NO_ENCONTRADA";
    public const string CodigoEvaluacionFueraDeAlcance = "EVALUACION_FUERA_DE_ALCANCE";
    public const string CodigoEvaluacionNoOficial = "EVALUACION_NO_OFICIAL";
    public const string CodigoFuncionNoIncluida = "FUNCION_NO_INCLUIDA";
    public const string CodigoDisputaAbierta = "DISPUTA_ABIERTA_EXISTENTE";
    public const string CodigoDisputaNoEncontrada = "DISPUTA_NO_ENCONTRADA";
    public const string CodigoDisputaResuelta = "DISPUTA_YA_RESUELTA";

    public async Task<DisputaDto> CrearAsync(Guid evaluacionId, CrearDisputaDto dto, CancellationToken ct)
    {
        var agente = usuarioActual.RequerirRol(RolEmpresa.Agente);
        await validadorCrear.ValidarOLanzarAsync(dto, ct);

        // El filtro global oculta las evaluaciones de otras empresas: 404.
        var evaluacion = await db.EvaluacionesIA.AsNoTracking().FirstOrDefaultAsync(e => e.Id == evaluacionId, ct)
            ?? throw ExcepcionNegocio.NoEncontrado(CodigoEvaluacionNoEncontrada, "La evaluación no existe.");

        if (!await db.Llamadas.AnyAsync(l => l.Id == evaluacion.LlamadaId && l.AgenteId == agente.UsuarioId, ct))
        {
            throw ExcepcionNegocio.Prohibido(CodigoEvaluacionFueraDeAlcance, "Solo puede disputar sus propias evaluaciones.");
        }

        if (!evaluacion.EsOficial)
        {
            throw ExcepcionNegocio.Conflicto(CodigoEvaluacionNoOficial,
                "La evaluación está pendiente de revisión del supervisor; todavía no se puede disputar.");
        }

        if (!await plan.TieneFuncionAsync(agente.EmpresaId, CodigosFuncionPlan.Disputas, ct))
        {
            throw ExcepcionNegocio.Prohibido(CodigoFuncionNoIncluida, "El plan contratado no incluye disputas.");
        }

        if (await db.Disputas.AnyAsync(d => d.EvaluacionId == evaluacionId && d.Estado == EstadoDisputa.Abierta, ct))
        {
            throw DisputaAbierta();
        }

        var disputa = new Disputa
        {
            EmpresaId = agente.EmpresaId,
            EvaluacionId = evaluacionId,
            Motivo = dto.Motivo!.Trim(),
            CreadaEn = reloj.GetUtcNow().UtcDateTime,
        };
        db.Disputas.Add(disputa);
        await auditoria.RegistrarAsync(agente.EmpresaId, agente.UsuarioId, "CrearDisputa", "Disputa", disputa.Id,
            despues: new { disputa.EvaluacionId, disputa.Motivo }, cancellationToken: ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Dos disputas simultáneas sobre la misma evaluación: el índice único parcial rechaza la segunda.
            db.LimpiarSeguimiento();
            if (await db.Disputas.AnyAsync(d => d.EvaluacionId == evaluacionId && d.Estado == EstadoDisputa.Abierta, ct))
            {
                throw DisputaAbierta();
            }

            throw;
        }

        return DisputaDto.Desde(disputa);
    }

    /// <summary>AdminEmpresa: todas las de su empresa. Supervisor: las de sus campañas. Agente: las suyas.</summary>
    public async Task<PaginaDto<DisputaDto>> ListarAsync(FiltroDisputasDto filtro, CancellationToken ct)
    {
        var usuario = usuarioActual.RequerirRol(RolEmpresa.AdminEmpresa, RolEmpresa.Supervisor, RolEmpresa.Agente);
        await validadorFiltro.ValidarOLanzarAsync(filtro, ct);

        var consulta = db.Disputas.AsNoTracking();
        consulta = usuario.Rol switch
        {
            RolEmpresa.Supervisor => consulta.Where(d => db.EvaluacionesIA.Any(e => e.Id == d.EvaluacionId
                && db.Llamadas.Any(l => l.Id == e.LlamadaId
                    && db.UsuariosCampana.Any(uc => uc.UsuarioId == usuario.UsuarioId && uc.CampanaId == l.Rubrica!.CampanaId)))),
            RolEmpresa.Agente => consulta.Where(d => db.EvaluacionesIA.Any(e => e.Id == d.EvaluacionId
                && db.Llamadas.Any(l => l.Id == e.LlamadaId && l.AgenteId == usuario.UsuarioId))),
            _ => consulta,
        };

        if (filtro.Estado is { } estado)
        {
            consulta = consulta.Where(d => d.Estado == estado);
        }

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderByDescending(d => d.CreadaEn)
            .ThenBy(d => d.Id)
            .Skip(filtro.Saltar)
            .Take(filtro.TamanoPagina)
            .Select(d => new DisputaDto(d.Id, d.EmpresaId, d.EvaluacionId, d.Estado, d.Motivo, d.Resolucion, d.ResueltaPorId, d.CreadaEn, d.ResueltaEn))
            .ToListAsync(ct);

        return new PaginaDto<DisputaDto>(items, filtro.Pagina, filtro.TamanoPagina, total);
    }

    public async Task<DisputaDto> ResolverAsync(Guid disputaId, ResolverDisputaDto dto, CancellationToken ct)
    {
        var supervisor = usuarioActual.RequerirRol(RolEmpresa.Supervisor);
        await validadorResolver.ValidarOLanzarAsync(dto, ct);

        var disputa = await db.Disputas.AsNoTracking().FirstOrDefaultAsync(d => d.Id == disputaId, ct)
            ?? throw ExcepcionNegocio.NoEncontrado(CodigoDisputaNoEncontrada, "La disputa no existe.");

        // 403 CAMPANA_NO_ASIGNADA si el supervisor no está asignado a la campaña de la evaluación.
        await revision.VerificarSupervisorAsignadoAsync(disputa.EvaluacionId, supervisor.UsuarioId, ct);

        var resolucion = dto.Resolucion!.Trim();
        var ahora = reloj.GetUtcNow().UtcDateTime;

        await using var transaccion = await db.BeginTransactionAsync(ct);
        try
        {
            // Se reserva la disputa con un UPDATE condicionado: de dos resoluciones simultáneas solo una la encuentra Abierta.
            var filas = await db.Disputas
                .Where(d => d.Id == disputaId && d.Estado == EstadoDisputa.Abierta)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.Estado, EstadoDisputa.Resuelta)
                    .SetProperty(d => d.Resolucion, resolucion)
                    .SetProperty(d => d.ResueltaPorId, supervisor.UsuarioId)
                    .SetProperty(d => d.ResueltaEn, ahora), ct);
            if (filas == 0)
            {
                throw ExcepcionNegocio.Conflicto(CodigoDisputaResuelta, "La disputa ya fue resuelta.");
            }

            var nuevaRevision = dto.Decision == DecisionDisputa.Modificar
                ? await revision.AjustarAsync(disputa.EvaluacionId, supervisor.UsuarioId, new AjustarEvaluacionDto
                {
                    RevisionEsperada = dto.RevisionEsperada,
                    Motivo = resolucion,
                    Criterios = dto.Criterios,
                }, ct)
                : await revision.RegistrarDecisionSinCambiosAsync(disputa.EvaluacionId, supervisor.UsuarioId,
                    dto.RevisionEsperada!.Value, $"Disputa mantenida: {resolucion}", ct);

            await auditoria.RegistrarAsync(supervisor.EmpresaId, supervisor.UsuarioId, "ResolverDisputa", "Disputa", disputaId,
                new { Estado = EstadoDisputa.Abierta.ToString() },
                new { Estado = EstadoDisputa.Resuelta.ToString(), Decision = dto.Decision.ToString(), Resolucion = resolucion, Revision = nuevaRevision },
                ct);
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
        }
        catch
        {
            db.LimpiarSeguimiento();
            throw;
        }

        var resuelta = await db.Disputas.AsNoTracking().FirstAsync(d => d.Id == disputaId, ct);
        return DisputaDto.Desde(resuelta);
    }

    private static ExcepcionNegocio DisputaAbierta() =>
        ExcepcionNegocio.Conflicto(CodigoDisputaAbierta, "Ya existe una disputa abierta para esta evaluación.");
}
