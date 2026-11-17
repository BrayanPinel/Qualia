using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Qualia.API.Controllers;
using Qualia.Application;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Application.Features.Suscripciones.Consumo.ConsultarConsumo;
using Qualia.Application.Features.Suscripciones.ObtenerSuscripcion;
using Qualia.Application.Features.Suscripciones.PagoSimulado;
using Qualia.Application.Features.Suscripciones.Planes;
using Qualia.Domain.Suscripciones;
using Qualia.Infrastructure.Persistence.Seeders;

namespace Qualia.Tests.API;

public sealed class SuscripcionesControllersTests
{
    private static readonly DateTime AhoraFijoUtc = new(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task EndpointsP0_PlanesSuscripcionConsumoYPagoSimulado_CumplenContrato()
    {
        var repo = new RepositorioMemoria();
        var contextoAmbiente = new ContextoEmpresaPrueba();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new RelojFijo(AhoraFijoUtc));
        services.AddApplication();
        services.AddScoped<IRepositorioSuscripciones>(_ => repo);
        services.AddScoped<IContextoEmpresa>(_ => contextoAmbiente);

        using var provider = services.BuildServiceProvider();

        // 1. GET /api/planes -> 200 PlanDto[]
        var planesController = new PlanesController(provider.GetRequiredService<ListarPlanesHandler>());
        var planesResult = Assert.IsType<OkObjectResult>(
            await planesController.ObtenerPlanes(CancellationToken.None));
        var planesDto = Assert.IsAssignableFrom<IReadOnlyList<PlanDto>>(planesResult.Value);
        Assert.Equal(3, planesDto.Count);

        // Registrar suscripción inicial para Empresa A.
        var empresaA = Guid.NewGuid();
        var empresaB = Guid.NewGuid();
        var creador = provider.GetRequiredService<ICrearSuscripcionInicial>();
        await creador.CrearInicialAsync(empresaA, PlanesCatalogo.ProId, AhoraFijoUtc);

        // 2. GET /api/suscripcion para Empresa B (sin suscripción / aislamiento) -> 404
        var suscripcionControllerB = CrearSuscripcionController(provider, contextoAmbiente, empresaB);
        var noEncontrada = Assert.IsType<ObjectResult>(
            await suscripcionControllerB.ObtenerSuscripcion(CancellationToken.None));
        Assert.Equal(StatusCodes.Status404NotFound, noEncontrada.StatusCode);

        // 3. GET /api/suscripcion para Empresa A -> 200 SuscripcionDto con PeriodoActual
        var suscripcionControllerA = CrearSuscripcionController(provider, contextoAmbiente, empresaA);
        var suscripcionOk = Assert.IsType<OkObjectResult>(
            await suscripcionControllerA.ObtenerSuscripcion(CancellationToken.None));
        var suscripcionDto = Assert.IsType<SuscripcionDto>(suscripcionOk.Value);
        Assert.Equal(empresaA, suscripcionDto.EmpresaId);
        Assert.Equal("Prueba", suscripcionDto.Estado);
        Assert.NotNull(suscripcionDto.PeriodoActual);

        // 4. POST /api/suscripcion/pago-simulado Rechazado -> 409 ErrorDto PAGO_RECHAZADO (no acredita minutos)
        var pagoRechazado = Assert.IsType<ObjectResult>(
            await suscripcionControllerA.RegistrarPagoSimulado(new PagoSimuladoDto("Rechazado"), CancellationToken.None));
        Assert.Equal(StatusCodes.Status409Conflict, pagoRechazado.StatusCode);
        var errorPago = Assert.IsType<ErrorDto>(pagoRechazado.Value);
        Assert.Equal(ErroresSuscripcion.CodigoPagoRechazado, errorPago.Codigo);

        // 5. POST /api/suscripcion/pago-simulado Aprobado -> 200 SuscripcionDto Activa
        var pagoAprobado = Assert.IsType<OkObjectResult>(
            await suscripcionControllerA.RegistrarPagoSimulado(new PagoSimuladoDto("Aprobado"), CancellationToken.None));
        var suscripcionActiva = Assert.IsType<SuscripcionDto>(pagoAprobado.Value);
        Assert.Equal("Activa", suscripcionActiva.Estado);

        // 6. GET /api/consumo?periodo=2026-11 -> 200 ConsumoDto
        contextoAmbiente.EmpresaIdActual = empresaA;
        var consumoControllerA = new ConsumoController(
            provider.GetRequiredService<ConsultarConsumoHandler>())
        {
            ControllerContext = CrearControllerContext(empresaA),
        };

        var consumoOk = Assert.IsType<OkObjectResult>(
            await consumoControllerA.ConsultarConsumo("2026-11", CancellationToken.None));
        var consumoDto = Assert.IsType<ConsumoDto>(consumoOk.Value);
        Assert.Equal(5000m, consumoDto.MinutosContratados);
        Assert.Equal(5000m, consumoDto.MinutosDisponibles);
    }

    private static SuscripcionController CrearSuscripcionController(
        ServiceProvider provider,
        ContextoEmpresaPrueba contextoAmbiente,
        Guid empresaId)
    {
        contextoAmbiente.EmpresaIdActual = empresaId;
        return new SuscripcionController(
            provider.GetRequiredService<ObtenerSuscripcionHandler>(),
            provider.GetRequiredService<RegistrarPagoSimuladoHandler>())
        {
            ControllerContext = CrearControllerContext(empresaId),
        };
    }

    private static ControllerContext CrearControllerContext(Guid empresaId)
    {
        var claims = new[]
        {
            new Claim("empresaId", empresaId.ToString()),
            new Claim(ClaimTypes.Role, "AdminEmpresa"),
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
            TraceIdentifier = "trace-test-001",
        };

        return new ControllerContext
        {
            HttpContext = httpContext,
        };
    }

    private sealed class RelojFijo(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class RepositorioMemoria : IRepositorioSuscripciones
    {
        private readonly IReadOnlyList<Plan> _planes = PlanesSeeder.CrearCatalogoInicial();
        private readonly List<Suscripcion> _suscripciones = [];
        private readonly List<PeriodoConsumo> _periodos = [];
        private readonly List<ConsumoLlamada> _consumos = [];

        public Task<IReadOnlyList<Plan>> ListarPlanesActivosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Plan>>(_planes.Where(p => p.Activo).OrderBy(p => p.PrecioMensual).ToArray());

        public Task<Plan?> ObtenerPlanAsync(Guid planId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_planes.FirstOrDefault(p => p.Id == planId));

        public Task<Suscripcion?> ObtenerSuscripcionActualAsync(Guid empresaId, CancellationToken cancellationToken = default) =>
            ObtenerSuscripcionActualSinContextoAsync(empresaId, cancellationToken);

        public Task<Suscripcion?> ObtenerSuscripcionActualSinContextoAsync(Guid empresaId, CancellationToken cancellationToken = default)
        {
            var actual = _suscripciones
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
            Task.FromResult(_periodos.FirstOrDefault(p => p.EmpresaId == empresaId && p.FechaFin == mes.FinUtc));

        public Task<PeriodoConsumo?> ObtenerPeriodoConBloqueoAsync(Guid empresaId, PeriodoMensual mes, CancellationToken cancellationToken = default) =>
            ObtenerPeriodoAsync(empresaId, mes, cancellationToken);

        public Task<TotalesConsumo> ObtenerTotalesConsumoAsync(Guid empresaId, Guid periodoId, CancellationToken cancellationToken = default)
        {
            var items = _consumos.Where(c => c.EmpresaId == empresaId && c.PeriodoId == periodoId).ToArray();
            return Task.FromResult(new TotalesConsumo(items.Sum(c => c.MinutosReservados), items.Sum(c => c.MinutosFacturables)));
        }

        public Task<bool> PeriodoTieneConsumosAsync(Guid empresaId, Guid periodoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_consumos.Any(c => c.EmpresaId == empresaId && c.PeriodoId == periodoId));

        public Task<ConsumoLlamada?> ObtenerConsumoLlamadaAsync(Guid empresaId, Guid llamadaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_consumos.FirstOrDefault(c => c.EmpresaId == empresaId && c.LlamadaId == llamadaId));

        public Task<bool> EmpresaExisteAsync(Guid empresaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<(IReadOnlyList<EmpresaComercialDto> Items, int Total)> ListarEmpresasComercialesAsync(
            int pagina,
            int tamanoPagina,
            PeriodoMensual mesActual,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<EmpresaComercialDto>, int)>(([], 0));

        public void Agregar(Suscripcion suscripcion) => _suscripciones.Add(suscripcion);
        public void Agregar(PeriodoConsumo periodo) => _periodos.Add(periodo);
        public void Agregar(ConsumoLlamada consumo) => _consumos.Add(consumo);
        public void Agregar(AuditoriaPlataforma auditoria) { }
        public Task<ITransaccion> IniciarTransaccionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ITransaccion>(new TransaccionNoOp());
        public Task GuardarCambiosAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private sealed class TransaccionNoOp : ITransaccion
        {
            public Task ConfirmarAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    /// <summary>Contexto de empresa de prueba: simula la empresa que IContextoEmpresa obtiene del token.</summary>
    private sealed class ContextoEmpresaPrueba : IContextoEmpresa
    {
        public Guid? EmpresaIdActual { get; set; }

        public void Establecer(Guid empresaId) => EmpresaIdActual = empresaId;
    }
}
