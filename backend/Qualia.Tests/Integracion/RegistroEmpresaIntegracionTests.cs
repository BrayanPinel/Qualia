using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Empresas;
using Qualia.Application.Features.Empresas.Dtos;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Application.Features.Suscripciones.CrearSuscripcionInicial;
using Qualia.Domain.Enums;
using Qualia.Infrastructure.Persistence;
using Qualia.Tests.Integracion.Infraestructura;
using Norte = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Norte;

namespace Qualia.Tests.Integracion;

/// POST /api/empresas contra PostgreSQL real: alta transaccional de empresa, AdminEmpresa y suscripción
/// de prueba (contrato de suscripciones), rechazo sin registros huérfanos y auditoría persistida.
[Collection(ColeccionPostgres.Nombre)]
public sealed class RegistroEmpresaIntegracionTests : IAsyncLifetime
{
    private const string Contrasena = "Contrasena-Segura-2026";
    private readonly PostgresFixture _postgres;
    private QualiaApiFactory _api;

    public RegistroEmpresaIntegracionTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        _api = Crear();
    }

    private QualiaApiFactory Crear(Action<IServiceCollection>? servicios = null) =>
        new(_postgres, $"qualia_registro_{Guid.NewGuid():N}", configurarServicios: servicios);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [FactConDocker]
    public async Task Registro_correcto_crea_empresa_admin_suscripcion_de_prueba_y_auditoria()
    {
        var planId = await PlanIdAsync();

        var respuesta = await RegistrarAsync(Dto("  Mi-Empresa ", " Admin@MiEmpresa.COM ", planId));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var registrada = await QualiaApiFactory.LeerAsync<EmpresaRegistradaDto>(respuesta);
        Assert.Equal("mi-empresa", registrada.CodigoEmpresa);
        Assert.Equal(registrada.EmpresaId, registrada.Suscripcion.EmpresaId);
        Assert.Equal(planId, registrada.Suscripcion.PlanId);
        Assert.Equal("Prueba", registrada.Suscripcion.Estado);
        Assert.NotNull(registrada.Suscripcion.PeriodoActual);
        Assert.DoesNotContain("hash", await respuesta.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        await using var alcance = _api.Services.CreateAsyncScope();
        var db = alcance.ServiceProvider.GetRequiredService<QualiaDbContext>();
        var hash = alcance.ServiceProvider.GetRequiredService<IServicioHashContrasena>();

        var empresa = await db.Empresas.SingleAsync(e => e.Id == registrada.EmpresaId);
        Assert.Equal(EstadoEmpresa.Activa, empresa.Estado);

        // Sin sesión no hay empresa en contexto: se ignora el filtro para inspeccionar lo guardado.
        var admin = await db.Usuarios.IgnoreQueryFilters().SingleAsync(u => u.EmpresaId == empresa.Id);
        Assert.Equal(registrada.UsuarioAdministradorId, admin.Id);
        Assert.Equal(RolEmpresa.AdminEmpresa, admin.Rol);
        Assert.Equal("admin@miempresa.com", admin.Correo);
        Assert.Equal(ResultadoVerificacionContrasena.Correcta, hash.Verificar(admin.PasswordHash, Contrasena));

        Assert.Equal(1, await db.Suscripciones.IgnoreQueryFilters().CountAsync(s => s.EmpresaId == empresa.Id));
        Assert.Equal(1, await db.PeriodosConsumo.IgnoreQueryFilters().CountAsync(p => p.EmpresaId == empresa.Id));
        Assert.False(await db.Administradores.AnyAsync(a => a.Correo == admin.Correo));

        var auditoria = await db.Auditorias.IgnoreQueryFilters().SingleAsync(a => a.EmpresaId == empresa.Id);
        Assert.Equal("RegistrarEmpresa", auditoria.Accion);
        Assert.Equal(admin.Id, auditoria.UsuarioId);
        Assert.Contains("mi-empresa", auditoria.Despues);
        Assert.DoesNotContain(Contrasena, auditoria.Despues!);
    }

    [FactConDocker]
    public async Task El_mismo_correo_puede_administrar_dos_empresas()
    {
        var planId = await PlanIdAsync();

        var primera = await RegistrarAsync(Dto("empresa-uno", "dueno@correo.com", planId));
        var segunda = await RegistrarAsync(Dto("empresa-dos", "DUENO@correo.com", planId));

        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
    }

    [FactConDocker]
    public async Task Codigo_duplicado_sin_distinguir_mayusculas_devuelve_409_sin_crear_nada()
    {
        var planId = await PlanIdAsync();
        Assert.Equal(HttpStatusCode.Created, (await RegistrarAsync(Dto("repetida", "uno@correo.com", planId))).StatusCode);
        var antes = await ContarAsync();

        var respuesta = await RegistrarAsync(Dto(" REPETIDA ", "otro@correo.com", planId));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(RegistroEmpresa.CodigoDuplicado, (await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta)).Codigo);
        Assert.Equal(antes, await ContarAsync());
    }

    [FactConDocker]
    public async Task Plan_inexistente_devuelve_400_y_revierte_empresa_y_usuario()
    {
        var antes = await ContarAsync();

        var respuesta = await RegistrarAsync(Dto("sin-plan", "admin@sinplan.com", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        // El error lo produce el contrato de suscripciones con su propio código de validación.
        var error = await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta);
        Assert.Contains(error.Errores!.Keys, k => k.Equals("planId", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(antes, await ContarAsync());
    }

    [FactConDocker]
    public async Task Datos_invalidos_devuelven_400_con_detalle_por_campo()
    {
        var antes = await ContarAsync();

        var respuesta = await RegistrarAsync(new RegistrarEmpresaDto
        {
            CodigoEmpresa = "no valido!",
            NombreEmpresa = "AB",
            NombreAdministrador = "Ana",
            Email = "no-es-correo",
            Password = "corta",
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var error = await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta);
        Assert.Equal("VALIDACION", error.Codigo);
        Assert.NotNull(error.Errores);
        foreach (var campo in new[] { "CodigoEmpresa", "NombreEmpresa", "PlanId", "Email", "Password" })
        {
            Assert.Contains(error.Errores!.Keys, k => k.Equals(campo, StringComparison.OrdinalIgnoreCase));
        }

        Assert.Equal(antes, await ContarAsync());
    }

    [FactConDocker]
    public async Task Un_campo_desconocido_se_rechaza()
    {
        var cliente = _api.ClienteAnonimo();
        var respuesta = await cliente.PostAsJsonAsync("/api/empresas", new
        {
            codigoEmpresa = "extra",
            nombreEmpresa = "Empresa extra",
            planId = await PlanIdAsync(),
            nombreAdministrador = "Admin Extra",
            email = "admin@extra.com",
            password = Contrasena,
            rol = "PlatformAdmin",
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [FactConDocker]
    public async Task Fallo_del_servicio_de_suscripcion_no_deja_registros_huerfanos()
    {
        await _api.DisposeAsync();
        _api = Crear(s =>
        {
            s.RemoveAll<ICrearSuscripcionInicial>();
            s.AddScoped<CrearSuscripcionInicialService>();
            s.AddScoped<ICrearSuscripcionInicial, SuscripcionQueFalla>();
        });
        var antes = await ContarAsync();

        var respuesta = await RegistrarAsync(Dto("falla-c", "admin@falla.com", SuscripcionQueFalla.PlanQueFalla));

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Equal(antes, await ContarAsync());
    }

    [FactConDocker]
    public async Task Las_operaciones_de_otros_modulos_quedan_auditadas_en_la_tabla()
    {
        var cliente = _api.ClienteComo(Norte.Admin);

        var respuesta = await cliente.PostAsJsonAsync("/api/campanas", new { nombre = "Campaña auditada", activa = true });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        await using var alcance = _api.Services.CreateAsyncScope();
        var db = alcance.ServiceProvider.GetRequiredService<QualiaDbContext>();
        var auditoria = await db.Auditorias.IgnoreQueryFilters()   // la prueba no tiene sesión propia
            .SingleAsync(a => a.Accion == "CrearCampana");
        Assert.Equal(Norte.Admin, auditoria.UsuarioId);
        Assert.Contains("Campaña auditada", auditoria.Despues);
    }

    private Task<HttpResponseMessage> RegistrarAsync(RegistrarEmpresaDto dto) =>
        _api.ClienteAnonimo().PostAsJsonAsync("/api/empresas", dto, QualiaApiFactory.Json);

    private async Task<Guid> PlanIdAsync()
    {
        var planes = await QualiaApiFactory.LeerAsync<List<PlanDto>>(await _api.ClienteAnonimo().GetAsync("/api/planes"));
        return planes.First().Id;
    }

    private async Task<(int Empresas, int Usuarios, int Suscripciones, int Auditorias)> ContarAsync()
    {
        await using var alcance = _api.Services.CreateAsyncScope();
        var db = alcance.ServiceProvider.GetRequiredService<QualiaDbContext>();
        // Conteos globales de todas las empresas: se ignora el filtro a propósito.
        return (
            await db.Empresas.CountAsync(),
            await db.Usuarios.IgnoreQueryFilters().CountAsync(),
            await db.Suscripciones.IgnoreQueryFilters().CountAsync(),
            await db.Auditorias.IgnoreQueryFilters().CountAsync());
    }

    private static RegistrarEmpresaDto Dto(string codigo, string email, Guid planId) => new()
    {
        CodigoEmpresa = codigo,
        NombreEmpresa = $"Empresa {codigo.Trim()}",
        PlanId = planId,
        NombreAdministrador = "Administrador Principal",
        Email = email,
        Password = Contrasena,
    };

    /// Falla solo con PlanQueFalla; el resto (p. ej. el sembrador demo) usa el servicio real.
    private sealed class SuscripcionQueFalla(CrearSuscripcionInicialService real) : ICrearSuscripcionInicial
    {
        public static readonly Guid PlanQueFalla = Guid.Parse("0badc0de-0000-4000-8000-000000000001");

        public Task<Resultado<SuscripcionDto>> CrearInicialAsync(
            Guid empresaId, Guid planId, DateTime fechaRegistroUtc, CancellationToken cancellationToken = default) =>
            planId == PlanQueFalla
                ? throw new InvalidOperationException("Fallo simulado del servicio de suscripciones.")
                : real.CrearInicialAsync(empresaId, planId, fechaRegistroUtc, cancellationToken);
    }
}
