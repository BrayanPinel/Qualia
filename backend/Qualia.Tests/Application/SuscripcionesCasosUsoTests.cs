using Microsoft.Extensions.DependencyInjection;
using Qualia.Application;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Models;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Application.Features.Suscripciones.Consumo.ConsultarConsumo;
using Qualia.Application.Features.Suscripciones.ObtenerSuscripcion;
using Qualia.Application.Features.Suscripciones.PagoSimulado;
using Qualia.Application.Features.Suscripciones.Planes;
using Qualia.Application.Features.Suscripciones.Plataforma.CambiarSuscripcion;
using Qualia.Application.Features.Suscripciones.Plataforma.ListarEmpresasComerciales;
using Qualia.Domain.Suscripciones;

namespace Qualia.Tests.Application;

public sealed class SuscripcionesCasosUsoTests
{
    private static readonly DateTime AhoraFijoUtc = new(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RegistroInicialYPagoSimulado_NoDuplicaPeriodoNiMinutos()
    {
        var repo = new RepositorioSuscripcionesEnMemoria();
        var empresaId = Guid.NewGuid();
        var contexto = new ContextoEmpresaFijo(empresaId);
        var auditoria = new RegistroAuditoriaEnMemoria();
        var provider = CrearServiceProvider(repo, contexto, auditoria);

        var creador = provider.GetRequiredService<ICrearSuscripcionInicial>();
        var inicial = await creador.CrearInicialAsync(empresaId, PlanesCatalogo.BasicoId, AhoraFijoUtc);
        Assert.True(inicial.EsExitoso);
        Assert.Equal("Prueba", inicial.Valor.Estado);
        Assert.NotNull(inicial.Valor.PeriodoActual);
        Assert.Equal(1000.00m, inicial.Valor.PeriodoActual.MinutosContratados);

        // No permite una segunda suscripción vigente.
        var duplicada = await creador.CrearInicialAsync(empresaId, PlanesCatalogo.ProId, AhoraFijoUtc);
        Assert.False(duplicada.EsExitoso);
        Assert.Equal(ErroresSuscripcion.CodigoSuscripcionYaExiste, duplicada.Error.Codigo);

        var pagoHandler = provider.GetRequiredService<RegistrarPagoSimuladoHandler>();
        var primerPago = await pagoHandler.EjecutarAsync(new PagoSimuladoDto("Aprobado"));
        Assert.True(primerPago.EsExitoso);
        Assert.Equal("Activa", primerPago.Valor.Estado);

        var segundoPago = await pagoHandler.EjecutarAsync(new PagoSimuladoDto("Aprobado"));
        Assert.True(segundoPago.EsExitoso);
        Assert.Single(repo.Periodos);
        Assert.Equal(2, auditoria.Registros.Count);
    }

    [Fact]
    public async Task ConsumoEvaluacion_ReservaIdempotenteCuotaYCierreCorrectos()
    {
        var repo = new RepositorioSuscripcionesEnMemoria();
        var empresaId = Guid.NewGuid();
        var llamadaId = Guid.NewGuid();
        var provider = CrearServiceProvider(repo, new ContextoEmpresaFijo(empresaId));

        var creador = provider.GetRequiredService<ICrearSuscripcionInicial>();
        await creador.CrearInicialAsync(empresaId, PlanesCatalogo.BasicoId, AhoraFijoUtc);

        var consumoService = provider.GetRequiredService<IConsumoEvaluacion>();

        // 61 seg -> 1.02 min
        var primera = await consumoService.ReservarAsync(new ReservarConsumoRequest(empresaId, llamadaId, 61m, AhoraFijoUtc));
        var reintento = await consumoService.ReservarAsync(new ReservarConsumoRequest(empresaId, llamadaId, 61m, AhoraFijoUtc));

        Assert.True(primera.EsExitoso);
        Assert.True(reintento.EsExitoso);
        Assert.Equal(primera.Valor.Id, reintento.Valor.Id);
        Assert.Single(repo.Consumos);

        // Exceder la cuota restante (1000 - 1.02 = 998.98) debe devolver 409 CUOTA_AGOTADA.
        var llamadaExcesiva = Guid.NewGuid();
        var agotada = await consumoService.ReservarAsync(
            new ReservarConsumoRequest(empresaId, llamadaExcesiva, 60000m, AhoraFijoUtc));

        Assert.False(agotada.EsExitoso);
        Assert.Equal(ErroresSuscripcion.CodigoCuotaAgotada, agotada.Error.Codigo);

        var usoTecnico = await consumoService.RegistrarUsoTecnicoAsync(
            new RegistrarUsoTecnicoRequest(empresaId, llamadaId, 500, 1200, 300, 0.015m));
        Assert.True(usoTecnico.EsExitoso);

        var facturado = await consumoService.FacturarAsync(empresaId, llamadaId);
        Assert.True(facturado.EsExitoso);

        var consultaConsumo = provider.GetRequiredService<ConsultarConsumoHandler>();
        var resumen = await consultaConsumo.EjecutarAsync(new ConsultarConsumoQuery("2026-11"));
        Assert.True(resumen.EsExitoso);
        Assert.Equal(0m, resumen.Valor.MinutosReservados);
        Assert.Equal(1.02m, resumen.Valor.MinutosFacturables);
        Assert.Equal(998.98m, resumen.Valor.MinutosDisponibles);
    }

    [Fact]
    public async Task CambiarSuscripcion_BloqueaSiPeriodoTieneUsoInclusoLiberadoYPermiteSiEstaVacio()
    {
        var repo = new RepositorioSuscripcionesEnMemoria();
        var empresaId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        repo.EmpresasExistentes.Add(empresaId);

        var provider = CrearServiceProvider(repo, new ContextoEmpresaFijo(empresaId));
        var creador = provider.GetRequiredService<ICrearSuscripcionInicial>();
        await creador.CrearInicialAsync(empresaId, PlanesCatalogo.BasicoId, AhoraFijoUtc);

        var cambiarHandler = provider.GetRequiredService<CambiarSuscripcionHandler>();

        // Periodo vacío: permite cambio de plan a Pro y sustituye la cuota del periodo.
        var cambioExitoso = await cambiarHandler.EjecutarAsync(new CambiarSuscripcionCommand(
            empresaId,
            adminId,
            new CambiarSuscripcionDto(
                PlanesCatalogo.ProId,
                "Activa",
                AhoraFijoUtc.AddMonths(1),
                true,
                "Actualización comercial aprobada por soporte.")));

        Assert.True(cambioExitoso.EsExitoso);
        Assert.Equal(PlanesCatalogo.ProId, cambioExitoso.Valor.PlanId);
        Assert.Equal(5000.00m, cambioExitoso.Valor.PeriodoActual!.MinutosContratados);
        Assert.Single(repo.AuditoriasPlataforma);

        // Registrar y liberar una llamada: sigue impidiendo cambio de plan en el mismo mes.
        var llamadaId = Guid.NewGuid();
        var consumoService = provider.GetRequiredService<IConsumoEvaluacion>();
        await consumoService.ReservarAsync(new ReservarConsumoRequest(empresaId, llamadaId, 120m, AhoraFijoUtc));
        await consumoService.LiberarAsync(empresaId, llamadaId);

        var cambioBloqueado = await cambiarHandler.EjecutarAsync(new CambiarSuscripcionCommand(
            empresaId,
            adminId,
            new CambiarSuscripcionDto(
                PlanesCatalogo.EnterpriseId,
                "Activa",
                AhoraFijoUtc.AddMonths(1),
                true,
                "Intento de upgrade con periodo en uso.")));

        Assert.False(cambioBloqueado.EsExitoso);
        Assert.Equal(ErroresSuscripcion.CodigoCambioPlanPeriodoEnUso, cambioBloqueado.Error.Codigo);
    }

    [Fact]
    public async Task ConsultaPlanYListado_VerificanLimitesFuncionesYPaginacion()
    {
        var repo = new RepositorioSuscripcionesEnMemoria();
        var empresaId = Guid.NewGuid();
        repo.EmpresasExistentes.Add(empresaId);
        var provider = CrearServiceProvider(repo, new ContextoEmpresaFijo(empresaId));

        var creador = provider.GetRequiredService<ICrearSuscripcionInicial>();
        await creador.CrearInicialAsync(empresaId, PlanesCatalogo.BasicoId, AhoraFijoUtc);

        var consultaPlan = provider.GetRequiredService<IConsultaPlanService>();
        Assert.True(await consultaPlan.TieneFuncionAsync(empresaId, CodigosFuncionPlan.Evaluacion));
        Assert.False(await consultaPlan.TieneFuncionAsync(empresaId, CodigosFuncionPlan.Disputas));
        Assert.True(await consultaPlan.AdmiteSupervisoresAsync(empresaId, 2));
        Assert.False(await consultaPlan.AdmiteSupervisoresAsync(empresaId, 3));
        Assert.True(await consultaPlan.AdmiteRubricasActivasAsync(empresaId, 1));
        Assert.False(await consultaPlan.AdmiteRubricasActivasAsync(empresaId, 2));

        var listarPlanes = provider.GetRequiredService<ListarPlanesHandler>();
        var planes = await listarPlanes.EjecutarAsync();
        Assert.Equal(3, planes.Count);

        var obtenerSuscripcion = provider.GetRequiredService<ObtenerSuscripcionHandler>();
        var sub = await obtenerSuscripcion.EjecutarAsync();
        Assert.True(sub.EsExitoso);

        var listarEmpresas = provider.GetRequiredService<ListarEmpresasComercialesHandler>();
        var pagina = await listarEmpresas.EjecutarAsync(new PaginacionQuery(1, 20));
        Assert.True(pagina.EsExitoso);
        Assert.Single(pagina.Valor.Items);
    }

    private static ServiceProvider CrearServiceProvider(
        RepositorioSuscripcionesEnMemoria repo,
        IContextoEmpresa contexto,
        IRegistroAuditoria? auditoria = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new RelojFijo(AhoraFijoUtc));
        services.AddSuscripcionesApplication();
        services.AddScoped<IRepositorioSuscripciones>(_ => repo);
        services.AddScoped<IContextoEmpresa>(_ => contexto);
        if (auditoria is not null)
        {
            services.AddScoped<IRegistroAuditoria>(_ => auditoria);
        }

        return services.BuildServiceProvider();
    }

    private sealed class RelojFijo(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class ContextoEmpresaFijo(Guid? empresaId) : IContextoEmpresa
    {
        public Guid? EmpresaIdActual { get; private set; } = empresaId;

        public void Establecer(Guid id) => EmpresaIdActual = id;
    }

    private sealed class RegistroAuditoriaEnMemoria : IRegistroAuditoria
    {
        public List<(Guid EmpresaId, string Accion)> Registros { get; } = [];

        public Task RegistrarAsync(
            Guid empresaId,
            Guid? usuarioId,
            string accion,
            string entidad,
            Guid? entidadId,
            object? antes = null,
            object? despues = null,
            CancellationToken cancellationToken = default)
        {
            Registros.Add((empresaId, accion));
            return Task.CompletedTask;
        }
    }

    private sealed class RepositorioSuscripcionesEnMemoria : IRepositorioSuscripciones
    {
        private readonly List<Plan> _planes = CrearPlanesCatalogo();

        public HashSet<Guid> EmpresasExistentes { get; } = [];
        public List<Suscripcion> Suscripciones { get; } = [];
        public List<PeriodoConsumo> Periodos { get; } = [];
        public List<ConsumoLlamada> Consumos { get; } = [];
        public List<AuditoriaPlataforma> AuditoriasPlataforma { get; } = [];

        public Task<IReadOnlyList<Plan>> ListarPlanesActivosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Plan>>(_planes.Where(p => p.Activo).OrderBy(p => p.PrecioMensual).ToArray());

        public Task<Plan?> ObtenerPlanAsync(Guid planId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_planes.FirstOrDefault(p => p.Id == planId));

        public Task<Suscripcion?> ObtenerSuscripcionActualAsync(Guid empresaId, CancellationToken cancellationToken = default) =>
            ObtenerSuscripcionActualSinContextoAsync(empresaId, cancellationToken);

        public Task<Suscripcion?> ObtenerSuscripcionActualSinContextoAsync(Guid empresaId, CancellationToken cancellationToken = default)
        {
            var actual = Suscripciones
                .Where(s => s.EmpresaId == empresaId)
                .OrderByDescending(s => s.EsVigente)
                .ThenByDescending(s => s.FechaInicio)
                .FirstOrDefault();

            if (actual is not null && actual.Plan is null)
            {
                var plan = _planes.First(p => p.Id == actual.PlanId);
                typeof(Suscripcion).GetProperty(nameof(Suscripcion.Plan))!.SetValue(actual, plan);
            }

            return Task.FromResult(actual);
        }

        public Task<Suscripcion?> BloquearSuscripcionActualAsync(Guid empresaId, CancellationToken cancellationToken = default) =>
            ObtenerSuscripcionActualSinContextoAsync(empresaId, cancellationToken);

        public Task<PeriodoConsumo?> ObtenerPeriodoAsync(Guid empresaId, PeriodoMensual mes, CancellationToken cancellationToken = default) =>
            Task.FromResult(Periodos.FirstOrDefault(p => p.EmpresaId == empresaId && p.FechaFin == mes.FinUtc));

        public Task<PeriodoConsumo?> ObtenerPeriodoConBloqueoAsync(Guid empresaId, PeriodoMensual mes, CancellationToken cancellationToken = default) =>
            ObtenerPeriodoAsync(empresaId, mes, cancellationToken);

        public Task<TotalesConsumo> ObtenerTotalesConsumoAsync(Guid empresaId, Guid periodoId, CancellationToken cancellationToken = default)
        {
            var delPeriodo = Consumos.Where(c => c.EmpresaId == empresaId && c.PeriodoId == periodoId).ToArray();
            return Task.FromResult(new TotalesConsumo(
                delPeriodo.Sum(c => c.MinutosReservados),
                delPeriodo.Sum(c => c.MinutosFacturables)));
        }

        public Task<bool> PeriodoTieneConsumosAsync(Guid empresaId, Guid periodoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Consumos.Any(c => c.EmpresaId == empresaId && c.PeriodoId == periodoId));

        public Task<ConsumoLlamada?> ObtenerConsumoLlamadaAsync(Guid empresaId, Guid llamadaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Consumos.FirstOrDefault(c => c.EmpresaId == empresaId && c.LlamadaId == llamadaId));

        public Task<bool> EmpresaExisteAsync(Guid empresaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(EmpresasExistentes.Contains(empresaId));

        public async Task<(IReadOnlyList<EmpresaComercialDto> Items, int Total)> ListarEmpresasComercialesAsync(
            int pagina,
            int tamanoPagina,
            PeriodoMensual mesActual,
            CancellationToken cancellationToken = default)
        {
            var lista = new List<EmpresaComercialDto>();
            foreach (var empresaId in EmpresasExistentes)
            {
                var sub = await ObtenerSuscripcionActualSinContextoAsync(empresaId, cancellationToken);
                var per = await ObtenerPeriodoAsync(empresaId, mesActual, cancellationToken);
                var totales = per is null ? TotalesConsumo.Cero : await ObtenerTotalesConsumoAsync(empresaId, per.Id, cancellationToken);
                var saldo = per is null ? SaldoConsumo.Vacio : new SaldoConsumo(per.MinutosContratados, totales.MinutosReservados, totales.MinutosFacturables);

                lista.Add(new EmpresaComercialDto(
                    empresaId,
                    $"EMP-{empresaId.ToString()[..4]}",
                    "Empresa Demo",
                    "Activa",
                    sub?.ASuscripcionDto(per),
                    saldo.AConsumoDto(empresaId, mesActual)));
            }

            var items = lista.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToArray();
            return (items, lista.Count);
        }

        public void Agregar(Suscripcion suscripcion) => Suscripciones.Add(suscripcion);
        public void Agregar(PeriodoConsumo periodo) => Periodos.Add(periodo);
        public void Agregar(ConsumoLlamada consumo) => Consumos.Add(consumo);
        public void Agregar(AuditoriaPlataforma auditoria) => AuditoriasPlataforma.Add(auditoria);
        public Task<ITransaccion> IniciarTransaccionAsync(CancellationToken cancellationToken = default) => Task.FromResult<ITransaccion>(new TransaccionNoOp());
        public Task GuardarCambiosAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private static List<Plan> CrearPlanesCatalogo()
        {
            var basico = new Plan(PlanesCatalogo.BasicoId, "Básico", 29m, 1000m, 0.05m, 2, 1, 365);
            basico.AgregarFuncion(CodigosFuncionPlan.Evaluacion);
            basico.AgregarFuncion(CodigosFuncionPlan.ScorecardBasico);

            var pro = new Plan(PlanesCatalogo.ProId, "Pro", 99m, 5000m, 0.04m, 10, null, 365);
            foreach (var codigo in CodigosFuncionPlan.Todos)
            {
                pro.AgregarFuncion(codigo);
            }

            var enterprise = new Plan(PlanesCatalogo.EnterpriseId, "Enterprise", 299m, 20000m, 0.03m, null, null, 365);
            foreach (var codigo in CodigosFuncionPlan.Todos)
            {
                enterprise.AgregarFuncion(codigo);
            }

            return [basico, pro, enterprise];
        }

        private sealed class TransaccionNoOp : ITransaccion
        {
            public Task ConfirmarAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
