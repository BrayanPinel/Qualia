using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;
using Qualia.Domain.Servicios;

namespace Qualia.Application.Features.Llamadas;

/// <summary>
/// Casos de uso de lectura del módulo de llamadas:
/// listado, detalle, transcripción, evaluación y acceso temporal al audio.
/// </summary>
public sealed class ConsultasLlamadas(
    IQualiaDbContext db,
    AlcanceLlamadas alcance,
    IValidator<FiltroLlamadasDto> validadorFiltro,
    IConsultaRevisionVigente revisiones,
    IGeneradorUrlAudio generadorUrlAudio,
    IRegistroAuditoria auditoria)
{
    /// <summary>Proyección a LlamadaDto: campaña y versión salen de la rúbrica (sin FK duplicada a campaña).</summary>
    private static readonly Expression<Func<Llamada, LlamadaDto>> AProyeccionDto = l => new LlamadaDto(
        l.Id,
        l.EmpresaId,
        l.Rubrica!.CampanaId,
        l.AgenteId,
        l.RubricaId,
        l.Rubrica.Version,
        l.DuracionSeg,
        l.Estado,
        l.FechaLlamada,
        l.Trabajo != null
        && (l.Trabajo.Estado == EstadoTrabajo.Fallido || l.Trabajo.Estado == EstadoTrabajo.ReintentoPendiente)
            ? l.Trabajo.UltimoError
            : null);

    public async Task<PaginaDto<LlamadaDto>> ListarAsync(FiltroLlamadasDto filtro, CancellationToken ct)
    {
        var usuario = alcance.RequerirLector();
        await ValidarAsync(filtro, ct);

        var consulta = alcance.Filtrar(db.Llamadas.AsNoTracking(), usuario);

        if (filtro.CampanaId is { } campanaId)
        {
            consulta = consulta.Where(l => l.Rubrica!.CampanaId == campanaId);
        }

        if (filtro.AgenteId is { } agenteId)
        {
            consulta = consulta.Where(l => l.AgenteId == agenteId);
        }

        if (filtro.Estado is not null)
        {
            var estado = Enum.Parse<EstadoLlamada>(filtro.Estado);
            consulta = consulta.Where(l => l.Estado == estado);
        }

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderByDescending(l => l.FechaLlamada)
            .ThenBy(l => l.Id)
            .Skip((filtro.Pagina - 1) * filtro.TamanoPagina)
            .Take(filtro.TamanoPagina)
            .Select(AProyeccionDto)
            .ToListAsync(ct);

        return new PaginaDto<LlamadaDto>(items, filtro.Pagina, filtro.TamanoPagina, total);
    }

    public async Task<LlamadaDto> ObtenerAsync(Guid llamadaId, CancellationToken ct)
    {
        await alcance.VerificarAccesoAsync(llamadaId, ct);

        return await db.Llamadas.AsNoTracking()
            .Where(l => l.Id == llamadaId)
            .Select(AProyeccionDto)
            .SingleAsync(ct);
    }

    public async Task<IReadOnlyList<SegmentoDto>> ObtenerTranscripcionAsync(Guid llamadaId, CancellationToken ct)
    {
        await alcance.VerificarAccesoAsync(llamadaId, ct);

        // Lista vacía mientras la llamada aún no se transcribe.
        return await db.SegmentosTranscripcion.AsNoTracking()
            .Where(s => s.LlamadaId == llamadaId)
            .OrderBy(s => s.Orden)
            .Select(s => new SegmentoDto(s.Id, s.EmpresaId, s.Orden, s.Hablante, s.InicioSeg, s.FinSeg, s.Texto))
            .ToListAsync(ct);
    }

    public async Task<EvaluacionDto> ObtenerEvaluacionAsync(Guid llamadaId, CancellationToken ct)
    {
        await alcance.VerificarAccesoAsync(llamadaId, ct);

        var evaluacion = await db.EvaluacionesIA.AsNoTracking()
            .Include(e => e.Detalles).ThenInclude(d => d.Criterio)
            .Include(e => e.Detalles).ThenInclude(d => d.Evidencias)
            .FirstOrDefaultAsync(e => e.LlamadaId == llamadaId, ct)
            ?? throw ExcepcionNegocio.NoEncontrado("EVALUACION_NO_DISPONIBLE", "La evaluación de esta llamada todavía no está disponible.");

        var segmentos = await db.SegmentosTranscripcion.AsNoTracking()
            .Where(s => s.LlamadaId == llamadaId)
            .ToDictionaryAsync(s => s.Id, ct);

        var ajuste = await revisiones.ObtenerAjusteVigenteAsync(evaluacion.EmpresaId, evaluacion.Id, ct);

        var criterios = evaluacion.Detalles
            .OrderByDescending(d => d.Criterio!.Peso)
            .ThenBy(d => d.Criterio!.Nombre)
            .Select(d => MapearCriterio(d, ajuste, segmentos))
            .ToList();

        var total = CalculadoraPuntaje.Total(criterios.Select(c => (c.PuntajeVigente, c.Peso)));

        return new EvaluacionDto(
            evaluacion.Id,
            evaluacion.EmpresaId,
            evaluacion.LlamadaId,
            evaluacion.Estado,
            evaluacion.Revision,
            evaluacion.ModoIA,
            evaluacion.ModeloUsado,
            evaluacion.RequiereRevision,
            total,
            criterios);
    }

    public async Task<AccesoAudioDto> ObtenerAccesoAudioAsync(Guid llamadaId, string urlBase, CancellationToken ct)
    {
        var usuario = await alcance.VerificarAccesoAsync(llamadaId, ct);

        var acceso = generadorUrlAudio.Generar(usuario.EmpresaId, llamadaId, urlBase);

        await auditoria.RegistrarAsync(
            usuario.EmpresaId, usuario.UsuarioId, "AccesoAudio", "Llamada", llamadaId,
            despues: new { acceso.ExpiraEn }, cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        return acceso;
    }

    private static ResultadoCriterioDto MapearCriterio(
        DetalleEvaluacion detalle,
        AjusteVigente? ajuste,
        IReadOnlyDictionary<Guid, SegmentoTranscripcion> segmentos)
    {
        var criterio = detalle.Criterio!;
        CriterioAjustado? humano = null;
        ajuste?.CriteriosPorDetalle.TryGetValue(detalle.Id, out humano);

        var vigente = humano?.Puntaje ?? detalle.PuntajeCriterio;

        var evidencias = humano is not null
            ? humano.Evidencias.Select(e => (e.SegmentoId, e.CitaTextual))
            : detalle.Evidencias.Select(e => (e.SegmentoId, e.CitaTextual));

        return new ResultadoCriterioDto(
            criterio.Id,
            criterio.Nombre,
            criterio.Peso,
            detalle.PuntajeCriterio,
            humano?.Puntaje,
            vigente,
            humano?.Justificacion ?? detalle.Observacion,
            CalculadoraPuntaje.EsFalloCritico(criterio.EsCritico, vigente, criterio.UmbralCumplimiento),
            evidencias
                .Where(e => segmentos.ContainsKey(e.SegmentoId))
                .Select(e => new EvidenciaDto(e.SegmentoId, e.CitaTextual, segmentos[e.SegmentoId].InicioSeg, segmentos[e.SegmentoId].FinSeg))
                .OrderBy(e => e.InicioSeg)
                .ToList());
    }

    private async Task ValidarAsync(FiltroLlamadasDto filtro, CancellationToken ct)
    {
        var resultado = await validadorFiltro.ValidateAsync(filtro, ct);
        if (!resultado.IsValid)
        {
            throw ExcepcionNegocio.Validacion(resultado.ToDictionary());
        }
    }
}
