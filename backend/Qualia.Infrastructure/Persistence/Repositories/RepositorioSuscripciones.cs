using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Domain.Suscripciones;

namespace Qualia.Infrastructure.Persistence.Repositories;

/// Implementación en EF Core / PostgreSQL de IRepositorioSuscripciones.
/// Comparte la instancia de QualiaDbContext del scope para participar
/// en la misma unidad de trabajo transaccional que otros módulos.
public sealed class RepositorioSuscripciones : IRepositorioSuscripciones
{
    private readonly QualiaDbContext _context;

    public RepositorioSuscripciones(QualiaDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Plan>> ListarPlanesActivosAsync(CancellationToken cancellationToken = default) =>
        await _context.Planes
            .AsNoTracking()
            .Include(p => p.Funciones)
            .Where(p => p.Activo)
            .OrderBy(p => p.PrecioMensual)
            .ToListAsync(cancellationToken);

    public async Task<Plan?> ObtenerPlanAsync(Guid planId, CancellationToken cancellationToken = default) =>
        await _context.Planes
            .AsNoTracking()
            .Include(p => p.Funciones)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

    public async Task<Suscripcion?> ObtenerSuscripcionActualAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        // Usa el DbSet con HasQueryFilter activo para asegurar aislamiento multiempresa.
        return await OrdenarSuscripcionActual(
                _context.Suscripciones
                    .AsNoTracking()
                    .Include(s => s.Plan)
                    .ThenInclude(p => p!.Funciones)
                    .Where(s => s.EmpresaId == empresaId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Suscripcion?> ObtenerSuscripcionActualSinContextoAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return await OrdenarSuscripcionActual(
                _context.Suscripciones
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Include(s => s.Plan)
                    .ThenInclude(p => p!.Funciones)
                    .Where(s => s.EmpresaId == empresaId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Suscripcion?> BloquearSuscripcionActualAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        Suscripcion? suscripcion;

        if (_context.Database.IsRelational() && _context.Database.CurrentTransaction is not null)
        {
            suscripcion = await _context.Suscripciones
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM qualia.suscripcion
                    WHERE empresa_id = {empresaId}
                    ORDER BY CASE WHEN estado IN ('Prueba', 'Activa', 'EnMora') THEN 0 ELSE 1 END,
                             fecha_inicio DESC
                    LIMIT 1
                    FOR UPDATE
                    """)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(cancellationToken);

            if (suscripcion is not null)
            {
                await _context.Entry(suscripcion)
                    .Reference(s => s.Plan)
                    .Query()
                    .Include(p => p.Funciones)
                    .LoadAsync(cancellationToken);
            }
        }
        else
        {
            suscripcion = await OrdenarSuscripcionActual(
                    _context.Suscripciones
                        .IgnoreQueryFilters()
                        .Include(s => s.Plan)
                        .ThenInclude(p => p!.Funciones)
                        .Where(s => s.EmpresaId == empresaId))
                .FirstOrDefaultAsync(cancellationToken);
        }

        return suscripcion;
    }

    public async Task<PeriodoConsumo?> ObtenerPeriodoAsync(
        Guid empresaId,
        PeriodoMensual mes,
        CancellationToken cancellationToken = default) =>
        await _context.PeriodosConsumo
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.EmpresaId == empresaId && p.FechaFin == mes.FinUtc,
                cancellationToken);

    public async Task<PeriodoConsumo?> ObtenerPeriodoConBloqueoAsync(
        Guid empresaId,
        PeriodoMensual mes,
        CancellationToken cancellationToken = default)
    {
        var finUtc = mes.FinUtc;

        if (_context.Database.IsRelational() && _context.Database.CurrentTransaction is not null)
        {
            return await _context.PeriodosConsumo
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM qualia.periodo_consumo
                    WHERE empresa_id = {empresaId}
                      AND fecha_fin = {finUtc}
                    LIMIT 1
                    FOR UPDATE
                    """)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(cancellationToken);
        }

        return await _context.PeriodosConsumo
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                p => p.EmpresaId == empresaId && p.FechaFin == finUtc,
                cancellationToken);
    }

    public async Task<TotalesConsumo> ObtenerTotalesConsumoAsync(
        Guid empresaId,
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        var acumulado = await _context.ConsumosLlamada
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.EmpresaId == empresaId && c.PeriodoId == periodoId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Reservados = g.Sum(c => c.MinutosReservados),
                Facturables = g.Sum(c => c.MinutosFacturables),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return acumulado is null
            ? TotalesConsumo.Cero
            : new TotalesConsumo(acumulado.Reservados, acumulado.Facturables);
    }

    public async Task<bool> PeriodoTieneConsumosAsync(
        Guid empresaId,
        Guid periodoId,
        CancellationToken cancellationToken = default) =>
        await _context.ConsumosLlamada
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(c => c.EmpresaId == empresaId && c.PeriodoId == periodoId, cancellationToken);

    public async Task<ConsumoLlamada?> ObtenerConsumoLlamadaAsync(
        Guid empresaId,
        Guid llamadaId,
        CancellationToken cancellationToken = default) =>
        await _context.ConsumosLlamada
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                c => c.EmpresaId == empresaId && c.LlamadaId == llamadaId,
                cancellationToken);

    public async Task<bool> EmpresaExisteAsync(Guid empresaId, CancellationToken cancellationToken = default) =>
        await _context.Suscripciones
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(s => s.EmpresaId == empresaId, cancellationToken);

    public async Task<(IReadOnlyList<EmpresaComercialDto> Items, int Total)> ListarEmpresasComercialesAsync(
        int pagina,
        int tamanoPagina,
        PeriodoMensual mesActual,
        CancellationToken cancellationToken = default)
    {
        var empresasQuery = _context.Suscripciones
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(s => s.EmpresaId)
            .Distinct()
            .OrderBy(id => id);

        var total = await empresasQuery.CountAsync(cancellationToken);
        if (total == 0)
        {
            return ([], 0);
        }

        var empresaIds = await empresasQuery
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(cancellationToken);

        var items = new List<EmpresaComercialDto>(empresaIds.Count);
        foreach (var empresaId in empresaIds)
        {
            var suscripcion = await ObtenerSuscripcionActualSinContextoAsync(empresaId, cancellationToken);
            var periodo = await ObtenerPeriodoAsync(empresaId, mesActual, cancellationToken);
            var totales = periodo is null
                ? TotalesConsumo.Cero
                : await ObtenerTotalesConsumoAsync(empresaId, periodo.Id, cancellationToken);

            var saldo = periodo is null
                ? SaldoConsumo.Vacio
                : new SaldoConsumo(periodo.MinutosContratados, totales.MinutosReservados, totales.MinutosFacturables);

            items.Add(new EmpresaComercialDto(
                empresaId,
                Codigo: empresaId.ToString("N")[..8].ToUpperInvariant(),
                Nombre: $"Empresa {empresaId.ToString("N")[..8].ToUpperInvariant()}",
                Estado: "Activa",
                Suscripcion: suscripcion?.ASuscripcionDto(periodo),
                Consumo: saldo.AConsumoDto(empresaId, mesActual)));
        }

        return (items, total);
    }

    public void Agregar(Suscripcion suscripcion) => _context.Suscripciones.Add(suscripcion);

    public void Agregar(PeriodoConsumo periodo) => _context.PeriodosConsumo.Add(periodo);

    public void Agregar(ConsumoLlamada consumo) => _context.ConsumosLlamada.Add(consumo);

    public void Agregar(AuditoriaPlataforma auditoria) => _context.AuditoriasPlataforma.Add(auditoria);

    public async Task<ITransaccion> IniciarTransaccionAsync(CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational() || _context.Database.CurrentTransaction is not null)
        {
            return TransaccionNoOp.Instancia;
        }

        var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new TransaccionEfCore(tx);
    }

    public Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    private static IOrderedQueryable<Suscripcion> OrdenarSuscripcionActual(IQueryable<Suscripcion> query) =>
        query
            .OrderBy(s =>
                s.Estado == EstadoSuscripcion.Prueba ||
                s.Estado == EstadoSuscripcion.Activa ||
                s.Estado == EstadoSuscripcion.EnMora
                    ? 0
                    : 1)
            .ThenByDescending(s => s.FechaInicio);

    private sealed class TransaccionEfCore(IDbContextTransaction transaccion) : ITransaccion
    {
        public Task ConfirmarAsync(CancellationToken cancellationToken = default) =>
            transaccion.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaccion.DisposeAsync();
    }

    private sealed class TransaccionNoOp : ITransaccion
    {
        public static readonly TransaccionNoOp Instancia = new();

        public Task ConfirmarAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
