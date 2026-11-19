using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Models;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Desempeno.Dtos;
using Qualia.Domain.Enums;
using Qualia.Domain.Servicios;
using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Desempeno;

/// Scorecards por agente y reporte agregado por campaña (HU-09, HU-10, RN-11).
/// Se calculan a partir de los hechos: llamadas del mes (por fecha_llamada UTC) con evaluación,
/// puntaje vigente (último ajuste humano o resultado de IA) y disputas abiertas.
/// Oficial: revisada por un supervisor o calificada por la IA sin fallas detectadas.
/// Elegible: oficial y sin disputa abierta. Los promedios exigen Empresa.minimo_muestra elegibles.
public sealed class ConsultasDesempeno(
    IQualiaDbContext db,
    IUsuarioActual usuarioActual,
    IConsultaRevisionVigente revisiones,
    IConsultaPlanService plan,
    IValidator<FiltroScorecardDto> validadorScorecard,
    IValidator<FiltroScorecardsCampanaDto> validadorScorecardsCampana,
    IValidator<FiltroReporteDto> validadorReporte,
    TimeProvider reloj)
{
    /// GET /api/empresas/{empresaId}/agentes/me/scorecard — el agente consulta su propio desempeño.
    public async Task<ScorecardDto> ScorecardPropioAsync(FiltroScorecardDto filtro, CancellationToken ct)
    {
        var agente = usuarioActual.RequerirRol(RolEmpresa.Agente);
        await validadorScorecard.ValidarOLanzarAsync(filtro, ct);

        var campanaId = filtro.CampanaId!.Value;
        await AsegurarCampanaAsync(campanaId, ct);
        await AsegurarFuncionAsync(agente.EmpresaId, CodigosFuncionPlan.ScorecardBasico, ct);

        var periodo = Periodo(filtro.Periodo);
        var contexto = await CargarContextoAsync(agente.EmpresaId, campanaId, periodo, ct);
        return contexto.Scorecard(agente.UsuarioId);
    }

    /// GET /api/empresas/{empresaId}/agentes/{id}/scorecard — AdminEmpresa o Supervisor asignado a la campaña.
    public async Task<ScorecardDto> ScorecardAgenteAsync(Guid agenteId, FiltroScorecardDto filtro, CancellationToken ct)
    {
        var usuario = usuarioActual.RequerirRol(RolEmpresa.AdminEmpresa, RolEmpresa.Supervisor);
        await validadorScorecard.ValidarOLanzarAsync(filtro, ct);

        var campanaId = filtro.CampanaId!.Value;
        await AsegurarCampanaAsync(campanaId, ct);
        await AsegurarAlcanceAsync(usuario, campanaId, ct);

        if (!await db.Usuarios.AnyAsync(u => u.Id == agenteId && u.Rol == RolEmpresa.Agente, ct))
        {
            throw ExcepcionNegocio.NoEncontrado("AGENTE_NO_ENCONTRADO", "El agente no existe.");
        }

        await AsegurarFuncionAsync(usuario.EmpresaId, CodigosFuncionPlan.ScorecardBasico, ct);

        var contexto = await CargarContextoAsync(usuario.EmpresaId, campanaId, Periodo(filtro.Periodo), ct);
        return contexto.Scorecard(agenteId);
    }

    /// GET /api/empresas/{empresaId}/campanas/{id}/scorecards — un scorecard por agente: asignados a la campaña
    /// o con llamadas evaluadas en ella durante el periodo. Ordenados por nombre.
    public async Task<PaginaDto<ScorecardDto>> ScorecardsCampanaAsync(Guid campanaId, FiltroScorecardsCampanaDto filtro, CancellationToken ct)
    {
        var usuario = usuarioActual.RequerirRol(RolEmpresa.AdminEmpresa, RolEmpresa.Supervisor);
        await validadorScorecardsCampana.ValidarOLanzarAsync(filtro, ct);

        await AsegurarCampanaAsync(campanaId, ct);
        await AsegurarAlcanceAsync(usuario, campanaId, ct);
        await AsegurarFuncionAsync(usuario.EmpresaId, CodigosFuncionPlan.ScorecardBasico, ct);

        var contexto = await CargarContextoAsync(usuario.EmpresaId, campanaId, Periodo(filtro.Periodo), ct);
        var conLlamadas = contexto.Actual.Select(h => h.AgenteId).Distinct().ToList();

        var agentes = db.Usuarios.AsNoTracking()
            .Where(u => u.Rol == RolEmpresa.Agente
                        && (conLlamadas.Contains(u.Id)
                            || db.UsuariosCampana.Any(uc => uc.UsuarioId == u.Id && uc.CampanaId == campanaId)));

        var total = await agentes.CountAsync(ct);
        var pagina = await agentes
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Id)
            .Skip(filtro.Saltar)
            .Take(filtro.TamanoPagina)
            .Select(u => u.Id)
            .ToListAsync(ct);

        return new PaginaDto<ScorecardDto>(
            pagina.Select(contexto.Scorecard).ToList(), filtro.Pagina, filtro.TamanoPagina, total);
    }

    /// GET /api/empresas/{empresaId}/reportes/campanas/{id} — agregado de la campaña; también para Auditor.
    public async Task<ReporteCampanaDto> ReporteCampanaAsync(Guid campanaId, FiltroReporteDto filtro, CancellationToken ct)
    {
        var usuario = usuarioActual.RequerirRol(RolEmpresa.AdminEmpresa, RolEmpresa.Supervisor, RolEmpresa.Auditor);
        await validadorReporte.ValidarOLanzarAsync(filtro, ct);

        await AsegurarCampanaAsync(campanaId, ct);
        await AsegurarAlcanceAsync(usuario, campanaId, ct);
        await AsegurarFuncionAsync(usuario.EmpresaId, CodigosFuncionPlan.ScorecardBasico, ct);

        var periodo = Periodo(filtro.Periodo);
        var minimo = await MinimoMuestraAsync(usuario.EmpresaId, ct);
        var hechos = await CargarHechosAsync(campanaId, periodo, ct);
        var elegibles = hechos.Where(h => h.Elegible).ToList();
        var suficiente = elegibles.Count >= minimo;

        return new ReporteCampanaDto(
            usuario.EmpresaId,
            campanaId,
            periodo.ToString(),
            elegibles.Count,
            suficiente,
            suficiente ? Promedio(elegibles) : null,
            elegibles.Sum(h => h.CriticosFallidos));
    }

    private async Task<ContextoCampana> CargarContextoAsync(Guid empresaId, Guid campanaId, PeriodoMensual periodo, CancellationToken ct)
    {
        var minimo = await MinimoMuestraAsync(empresaId, ct);
        var conTendencia = await plan.TieneFuncionAsync(empresaId, CodigosFuncionPlan.Tendencia, ct);
        var conComparacion = await plan.TieneFuncionAsync(empresaId, CodigosFuncionPlan.ComparacionCampanas, ct);

        var actual = await CargarHechosAsync(campanaId, periodo, ct);
        var anterior = conTendencia
            ? await CargarHechosAsync(campanaId, PeriodoMensual.DesdeFecha(periodo.InicioUtc.AddMonths(-1)), ct)
            : [];

        return new ContextoCampana(empresaId, campanaId, periodo, minimo, conTendencia, conComparacion, actual, anterior);
    }

    /// Hechos de las llamadas evaluadas de la campaña en el mes, con el puntaje vigente por criterio.
    private async Task<List<HechoLlamada>> CargarHechosAsync(Guid campanaId, PeriodoMensual periodo, CancellationToken ct)
    {
        var inicio = periodo.InicioUtc;
        var fin = periodo.FinUtc;

        var filas = await db.Llamadas.AsNoTracking()
            .Where(l => l.Rubrica!.CampanaId == campanaId
                        && l.FechaLlamada >= inicio && l.FechaLlamada < fin
                        && l.Evaluacion != null)
            .Select(l => new
            {
                l.AgenteId,
                l.RubricaId,
                l.Rubrica!.Version,
                l.Evaluacion!.EmpresaId,
                EvaluacionId = l.Evaluacion.Id,
                l.Evaluacion.Estado,
                l.Evaluacion.RequiereRevision,
                DisputaAbierta = db.Disputas.Any(d => d.EvaluacionId == l.Evaluacion.Id && d.Estado == EstadoDisputa.Abierta),
                Detalles = l.Evaluacion.Detalles.Select(d => new
                {
                    d.Id,
                    d.CriterioId,
                    d.PuntajeCriterio,
                    d.Criterio!.Nombre,
                    d.Criterio.Peso,
                    d.Criterio.EsCritico,
                    d.Criterio.UmbralCumplimiento,
                }).ToList(),
            })
            .ToListAsync(ct);

        var hechos = new List<HechoLlamada>(filas.Count);
        foreach (var f in filas)
        {
            // Solo una evaluación revisada por un humano puede tener ajuste vigente.
            var ajuste = f.Estado == EstadoEvaluacion.EvaluadaPorIA
                ? null
                : await revisiones.ObtenerAjusteVigenteAsync(f.EmpresaId, f.EvaluacionId, ct);

            var criterios = f.Detalles.Select(d =>
            {
                CriterioAjustado? humano = null;
                ajuste?.CriteriosPorDetalle.TryGetValue(d.Id, out humano);
                var vigente = humano?.Puntaje ?? d.PuntajeCriterio;
                return new CriterioVigente(
                    d.CriterioId, d.Nombre, vigente, d.Peso,
                    CalculadoraPuntaje.EsFalloCritico(d.EsCritico, vigente, d.UmbralCumplimiento));
            }).ToList();

            var oficial = f.Estado != EstadoEvaluacion.EvaluadaPorIA || !f.RequiereRevision;

            hechos.Add(new HechoLlamada(
                f.AgenteId,
                f.RubricaId,
                f.Version,
                oficial,
                oficial && !f.DisputaAbierta,
                CalculadoraPuntaje.Total(criterios.Select(c => (c.Puntaje, c.Peso))),
                criterios));
        }

        return hechos;
    }

    private async Task AsegurarCampanaAsync(Guid campanaId, CancellationToken ct)
    {
        // El filtro global oculta las campañas de otras empresas: 404.
        if (!await db.Campanas.AnyAsync(c => c.Id == campanaId, ct))
        {
            throw ExcepcionNegocio.NoEncontrado("CAMPANA_NO_ENCONTRADA", "La campaña no existe.");
        }
    }

    private async Task AsegurarAlcanceAsync(UsuarioEmpresa usuario, Guid campanaId, CancellationToken ct)
    {
        if (usuario.Rol == RolEmpresa.Supervisor
            && !await db.UsuariosCampana.AnyAsync(uc => uc.UsuarioId == usuario.UsuarioId && uc.CampanaId == campanaId, ct))
        {
            throw ExcepcionNegocio.Prohibido("CAMPANA_NO_ASIGNADA", "No está asignado a esta campaña.");
        }
    }

    private async Task AsegurarFuncionAsync(Guid empresaId, string funcion, CancellationToken ct)
    {
        if (!await plan.TieneFuncionAsync(empresaId, funcion, ct))
        {
            throw ExcepcionNegocio.Prohibido("FUNCION_NO_INCLUIDA", "El plan contratado no incluye esta función.");
        }
    }

    private async Task<int> MinimoMuestraAsync(Guid empresaId, CancellationToken ct) =>
        await db.Empresas.Where(e => e.Id == empresaId).Select(e => e.MinimoMuestra).FirstAsync(ct);

    private PeriodoMensual Periodo(string? texto) =>
        texto is not null && PeriodoMensual.TryParse(texto, out var periodo)
            ? periodo
            : PeriodoMensual.DesdeFecha(reloj.GetUtcNow().UtcDateTime);

    /// Media de los totales por llamada (no media de promedios), a dos decimales.
    private static decimal Promedio(IReadOnlyCollection<HechoLlamada> elegibles) =>
        Math.Round(elegibles.Average(h => h.Total), 2, MidpointRounding.AwayFromZero);

    private sealed record CriterioVigente(Guid CriterioId, string Nombre, decimal Puntaje, decimal Peso, bool FalloCritico);

    private sealed record HechoLlamada(
        Guid AgenteId, Guid RubricaId, int Version, bool Oficial, bool Elegible, decimal Total, IReadOnlyList<CriterioVigente> Criterios)
    {
        public int CriticosFallidos => Criterios.Count(c => c.FalloCritico);
    }

    /// Hechos ya cargados de una campaña y mes (y del mes anterior si el plan tiene tendencia).
    private sealed record ContextoCampana(
        Guid EmpresaId,
        Guid CampanaId,
        PeriodoMensual Periodo,
        int Minimo,
        bool ConTendencia,
        bool ConComparacion,
        IReadOnlyList<HechoLlamada> Actual,
        IReadOnlyList<HechoLlamada> Anterior)
    {
        public ScorecardDto Scorecard(Guid agenteId)
        {
            var propios = Actual.Where(h => h.AgenteId == agenteId).ToList();
            var elegibles = propios.Where(h => h.Elegible).ToList();
            var suficiente = elegibles.Count >= Minimo;
            decimal? promedio = suficiente ? Promedio(elegibles) : null;

            return new ScorecardDto(
                EmpresaId,
                agenteId,
                CampanaId,
                Periodo.ToString(),
                propios.Count,
                propios.Count(h => h.Oficial),
                elegibles.Count,
                suficiente,
                promedio,
                Tendencia(agenteId, promedio),
                PromedioCampana(),
                elegibles.Sum(h => h.CriticosFallidos),
                suficiente ? PromediosPorCriterio(elegibles) : []);
        }

        private decimal? Tendencia(Guid agenteId, decimal? promedioActual)
        {
            if (!ConTendencia || promedioActual is null)
            {
                return null;
            }

            var previos = Anterior.Where(h => h.AgenteId == agenteId && h.Elegible).ToList();
            return previos.Count >= Minimo ? promedioActual - Promedio(previos) : null;
        }

        private decimal? PromedioCampana()
        {
            if (!ConComparacion)
            {
                return null;
            }

            var elegibles = Actual.Where(h => h.Elegible).ToList();
            return elegibles.Count >= Minimo ? Promedio(elegibles) : null;
        }

        /// Cada grupo se identifica por rúbrica, versión y criterio: no se mezclan versiones.
        private static List<PromedioCriterioDto> PromediosPorCriterio(IEnumerable<HechoLlamada> elegibles) =>
            elegibles
                .SelectMany(h => h.Criterios.Select(c => (h.RubricaId, h.Version, Criterio: c)))
                .GroupBy(x => (x.RubricaId, x.Version, x.Criterio.CriterioId, x.Criterio.Nombre))
                .Select(g => new PromedioCriterioDto(
                    g.Key.RubricaId,
                    g.Key.Version,
                    g.Key.CriterioId,
                    g.Key.Nombre,
                    Math.Round(g.Average(x => x.Criterio.Puntaje), 2, MidpointRounding.AwayFromZero),
                    g.Count()))
                .OrderBy(p => p.Version)
                .ThenBy(p => p.Nombre)
                .ToList();
    }
}
