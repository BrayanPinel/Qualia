using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Models;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Autenticacion.Dtos;
using Qualia.Application.Features.Campanas.Dtos;
using Qualia.Application.Features.Empresas.Dtos;
using Qualia.Application.Features.Usuarios;
using Qualia.Domain.Enums;
using Qualia.Domain.Suscripciones;
using Qualia.Infrastructure.Persistence;
using Qualia.Infrastructure.Persistence.Semillas;
using Qualia.Tests.Integracion.Infraestructura;
using Norte = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Norte;
using Sur = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Sur;

namespace Qualia.Tests.Integracion;

/// <summary>
/// GET/POST/PUT /api/usuarios contra PostgreSQL real: alcance por empresa, unicidad del correo,
/// límite de supervisores del plan, último AdminEmpresa (también con peticiones simultáneas)
/// y revocación inmediata del token de un usuario desactivado.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class UsuariosIntegracionTests : IAsyncLifetime
{
    private const string Contrasena = "Contrasena-Segura-2026";
    private readonly QualiaApiFactory _api;

    public UsuariosIntegracionTests(PostgresFixture postgres)
    {
        _api = new QualiaApiFactory(postgres, $"qualia_usuarios_{Guid.NewGuid():N}");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [FactConDocker]
    public async Task Listar_devuelve_solo_los_usuarios_de_la_empresa_paginados()
    {
        var admin = _api.ClienteComo(Norte.Admin);

        var todos = await QualiaApiFactory.LeerAsync<PaginaDto<UsuarioDto>>(await admin.GetAsync("/api/usuarios?pagina=1&tamanoPagina=100"));
        var pagina = await QualiaApiFactory.LeerAsync<PaginaDto<UsuarioDto>>(await admin.GetAsync("/api/usuarios?pagina=2&tamanoPagina=2"));

        Assert.Equal(6, todos.Total);
        Assert.All(todos.Items, u => Assert.Equal(Norte.Empresa, u.EmpresaId));
        Assert.DoesNotContain(todos.Items, u => u.Id == Sur.Agente);
        Assert.Equal(todos.Items.Select(u => u.Nombre).Order(StringComparer.Ordinal), todos.Items.Select(u => u.Nombre));
        Assert.Equal(2, pagina.Items.Count);
        Assert.Equal(todos.Items.Skip(2).Take(2).Select(u => u.Id), pagina.Items.Select(u => u.Id));
        Assert.DoesNotContain("hash", await (await admin.GetAsync("/api/usuarios")).Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [FactConDocker]
    public async Task Solo_AdminEmpresa_administra_usuarios()
    {
        var supervisora = _api.ClienteComo(Norte.Supervisora);

        var listar = await supervisora.GetAsync("/api/usuarios");
        var crear = await supervisora.PostAsJsonAsync("/api/usuarios", Nuevo("Intento Supervisor", "intento@norte.demo", RolEmpresa.Agente), QualiaApiFactory.Json);

        Assert.Equal(HttpStatusCode.Forbidden, listar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, crear.StatusCode);
        Assert.Equal("ROL_NO_AUTORIZADO", (await QualiaApiFactory.LeerAsync<ErrorDto>(crear)).Codigo);
    }

    [FactConDocker]
    public async Task Crear_usuario_normaliza_hashea_audita_y_permite_iniciar_sesion()
    {
        var respuesta = await _api.ClienteComo(Norte.Admin).PostAsJsonAsync("/api/usuarios",
            Nuevo("  Nuevo Agente ", " Nuevo.Agente@Norte.DEMO ", RolEmpresa.Agente), QualiaApiFactory.Json);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creado = await QualiaApiFactory.LeerAsync<UsuarioDto>(respuesta);
        Assert.Equal(Norte.Empresa, creado.EmpresaId);
        Assert.Equal("Nuevo Agente", creado.Nombre);
        Assert.Equal("nuevo.agente@norte.demo", creado.Email);
        Assert.True(creado.Activo);

        await using (var alcance = _api.Services.CreateAsyncScope())
        {
            var db = alcance.ServiceProvider.GetRequiredService<QualiaDbContext>();
            // Inspección directa sin sesión: se ignora el filtro por empresa a propósito.
            var guardado = await db.Usuarios.IgnoreQueryFilters().SingleAsync(u => u.Id == creado.Id);
            var hash = alcance.ServiceProvider.GetRequiredService<IServicioHashContrasena>();
            Assert.Equal(ResultadoVerificacionContrasena.Correcta, hash.Verificar(guardado.PasswordHash, Contrasena));

            var auditoria = await db.Auditorias.IgnoreQueryFilters().SingleAsync(a => a.Accion == "CrearUsuario");
            Assert.Equal(Norte.Admin, auditoria.UsuarioId);
            Assert.Equal(creado.Id, auditoria.EntidadId);
            Assert.DoesNotContain(Contrasena, auditoria.Despues!);
        }

        var login = await LoginAsync("demo-norte", "nuevo.agente@norte.demo", Contrasena);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [FactConDocker]
    public async Task El_correo_es_unico_dentro_de_la_empresa_pero_no_entre_empresas()
    {
        var admin = _api.ClienteComo(Norte.Admin);

        var repetido = await admin.PostAsJsonAsync("/api/usuarios", Nuevo("Otra Ana", "ANA.ADMIN@norte.demo", RolEmpresa.Agente), QualiaApiFactory.Json);
        var deSur = await admin.PostAsJsonAsync("/api/usuarios", Nuevo("Adrián en Norte", "adrian.agente@sur.demo", RolEmpresa.Agente), QualiaApiFactory.Json);

        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal(GestionUsuarios.CodigoCorreoDuplicado, (await QualiaApiFactory.LeerAsync<ErrorDto>(repetido)).Codigo);
        Assert.Equal(HttpStatusCode.Created, deSur.StatusCode);
    }

    [FactConDocker]
    public async Task No_se_puede_crear_PlatformAdmin_ni_elegir_la_empresa_desde_el_cuerpo()
    {
        var admin = _api.ClienteComo(Norte.Admin);

        var platformAdmin = await admin.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Intruso", email = "intruso@norte.demo", password = Contrasena, rol = "PlatformAdmin",
        });
        var otraEmpresa = await admin.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Intruso", email = "intruso@norte.demo", password = Contrasena, rol = "Agente", empresaId = Sur.Empresa,
        });
        var datosInvalidos = await admin.PostAsJsonAsync("/api/usuarios", new { nombre = "AB", email = "no-es-correo", password = "corta" });

        Assert.Equal(HttpStatusCode.BadRequest, platformAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, otraEmpresa.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, datosInvalidos.StatusCode);
        var error = await QualiaApiFactory.LeerAsync<ErrorDto>(datosInvalidos);
        foreach (var campo in new[] { "Nombre", "Email", "Password", "Rol" })
        {
            Assert.Contains(error.Errores!.Keys, k => k.Equals(campo, StringComparison.OrdinalIgnoreCase));
        }
    }

    [FactConDocker]
    public async Task Actualizar_cambia_nombre_y_estado_y_lo_audita()
    {
        var respuesta = await _api.ClienteComo(Norte.Admin).PutAsJsonAsync($"/api/usuarios/{Norte.AgenteAlicia}",
            new { nombre = "Alicia Renombrada", activo = false });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        await using var alcance = _api.Services.CreateAsyncScope();
        var db = alcance.ServiceProvider.GetRequiredService<QualiaDbContext>();
        var alicia = await db.Usuarios.IgnoreQueryFilters().SingleAsync(u => u.Id == Norte.AgenteAlicia);
        Assert.Equal("Alicia Renombrada", alicia.Nombre);
        Assert.False(alicia.Activo);
        var auditoria = await db.Auditorias.IgnoreQueryFilters().SingleAsync(a => a.Accion == "ActualizarUsuario");
        Assert.True(JsonDocument.Parse(auditoria.Antes!).RootElement.GetProperty("activo").GetBoolean());
        Assert.False(JsonDocument.Parse(auditoria.Despues!).RootElement.GetProperty("activo").GetBoolean());
    }

    [FactConDocker]
    public async Task Un_usuario_de_otra_empresa_responde_404()
    {
        var respuesta = await _api.ClienteComo(Norte.Admin).PutAsJsonAsync($"/api/usuarios/{Sur.Agente}",
            new { nombre = "Secuestrado", activo = false });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(GestionUsuarios.CodigoUsuarioNoEncontrado, (await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta)).Codigo);
        await using var alcance = _api.Services.CreateAsyncScope();
        var adrian = await alcance.ServiceProvider.GetRequiredService<QualiaDbContext>()
            .Usuarios.IgnoreQueryFilters().SingleAsync(u => u.Id == Sur.Agente);
        Assert.True(adrian.Activo);
        Assert.NotEqual("Secuestrado", adrian.Nombre);
    }

    [FactConDocker]
    public async Task No_se_puede_desactivar_al_ultimo_AdminEmpresa_activo()
    {
        var admin = _api.ClienteComo(Norte.Admin);

        var unico = await admin.PutAsJsonAsync($"/api/usuarios/{Norte.Admin}", new { nombre = "Ana Administradora", activo = false });
        Assert.Equal(HttpStatusCode.Conflict, unico.StatusCode);
        Assert.Equal(GestionUsuarios.CodigoUltimoAdministrador, (await QualiaApiFactory.LeerAsync<ErrorDto>(unico)).Codigo);

        var segundo = await QualiaApiFactory.LeerAsync<UsuarioDto>(await admin.PostAsJsonAsync("/api/usuarios",
            Nuevo("Segundo Admin", "segundo.admin@norte.demo", RolEmpresa.AdminEmpresa), QualiaApiFactory.Json));
        var conOtroActivo = await admin.PutAsJsonAsync($"/api/usuarios/{segundo.Id}", new { nombre = "Segundo Admin", activo = false });
        Assert.Equal(HttpStatusCode.NoContent, conOtroActivo.StatusCode);

        var deNuevoUnico = await admin.PutAsJsonAsync($"/api/usuarios/{Norte.Admin}", new { nombre = "Ana Administradora", activo = false });
        Assert.Equal(HttpStatusCode.Conflict, deNuevoUnico.StatusCode);
    }

    [FactConDocker]
    public async Task Dos_desactivaciones_simultaneas_no_dejan_a_la_empresa_sin_AdminEmpresa()
    {
        var admin = _api.ClienteComo(Norte.Admin);
        var segundo = await QualiaApiFactory.LeerAsync<UsuarioDto>(await admin.PostAsJsonAsync("/api/usuarios",
            Nuevo("Segundo Admin", "segundo.admin@norte.demo", RolEmpresa.AdminEmpresa), QualiaApiFactory.Json));

        var respuestas = await Task.WhenAll(
            _api.ClienteComo(Norte.Admin).PutAsJsonAsync($"/api/usuarios/{Norte.Admin}", new { nombre = "Ana Administradora", activo = false }),
            _api.ClienteComo(segundo.Id).PutAsJsonAsync($"/api/usuarios/{segundo.Id}", new { nombre = "Segundo Admin", activo = false }));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
        await using var alcance = _api.Services.CreateAsyncScope();
        var activos = await alcance.ServiceProvider.GetRequiredService<QualiaDbContext>().Usuarios.IgnoreQueryFilters()
            .CountAsync(u => u.EmpresaId == Norte.Empresa && u.Rol == RolEmpresa.AdminEmpresa && u.Activo);
        Assert.Equal(1, activos);
    }

    [FactConDocker]
    public async Task El_plan_basico_limita_los_supervisores_activos_al_crear_y_al_reactivar()
    {
        var registro = await QualiaApiFactory.LeerAsync<EmpresaRegistradaDto>(await _api.ClienteAnonimo().PostAsJsonAsync("/api/empresas",
            new RegistrarEmpresaDto
            {
                CodigoEmpresa = "basica", NombreEmpresa = "Empresa Básica", PlanId = PlanesCatalogo.BasicoId,
                NombreAdministrador = "Admin Básica", Email = "admin@basica.com", Password = Contrasena,
            }, QualiaApiFactory.Json));
        var admin = _api.ClienteComo(registro.UsuarioAdministradorId);

        var primero = await QualiaApiFactory.LeerAsync<UsuarioDto>(await admin.PostAsJsonAsync("/api/usuarios", Nuevo("Supervisor Uno", "s1@basica.com", RolEmpresa.Supervisor), QualiaApiFactory.Json));
        var segundo = await admin.PostAsJsonAsync("/api/usuarios", Nuevo("Supervisor Dos", "s2@basica.com", RolEmpresa.Supervisor), QualiaApiFactory.Json);
        var tercero = await admin.PostAsJsonAsync("/api/usuarios", Nuevo("Supervisor Tres", "s3@basica.com", RolEmpresa.Supervisor), QualiaApiFactory.Json);
        var agente = await admin.PostAsJsonAsync("/api/usuarios", Nuevo("Agente Libre", "a1@basica.com", RolEmpresa.Agente), QualiaApiFactory.Json);

        Assert.Equal(HttpStatusCode.Created, segundo.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, tercero.StatusCode);
        Assert.Equal(GestionUsuarios.CodigoLimiteSupervisores, (await QualiaApiFactory.LeerAsync<ErrorDto>(tercero)).Codigo);
        Assert.Equal(HttpStatusCode.Created, agente.StatusCode);

        // Al desactivar uno se libera el cupo; reactivarlo con el cupo ocupado vuelve a chocar con el límite.
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/usuarios/{primero.Id}", new { nombre = "Supervisor Uno", activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/usuarios", Nuevo("Supervisor Tres", "s3@basica.com", RolEmpresa.Supervisor), QualiaApiFactory.Json)).StatusCode);
        var reactivar = await admin.PutAsJsonAsync($"/api/usuarios/{primero.Id}", new { nombre = "Supervisor Uno", activo = true });
        Assert.Equal(HttpStatusCode.Forbidden, reactivar.StatusCode);
    }

    [FactConDocker]
    public async Task Un_usuario_desactivado_pierde_el_acceso_aunque_su_token_no_haya_expirado()
    {
        var sesion = await QualiaApiFactory.LeerAsync<SesionDto>(await LoginAsync("demo-norte", "andres.agente@norte.demo", DatosDemo.Contrasena));
        var agente = _api.ClienteAnonimo();
        agente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await agente.GetAsync("/api/campanas")).StatusCode);

        await _api.ClienteComo(Norte.Admin).PutAsJsonAsync($"/api/usuarios/{Norte.AgenteAndres}", new { nombre = "Andrés Agente", activo = false });
        var despues = await agente.GetAsync("/api/campanas");

        Assert.Equal(HttpStatusCode.Unauthorized, despues.StatusCode);
        Assert.Equal("SESION_REVOCADA", (await QualiaApiFactory.LeerAsync<ErrorDto>(despues)).Codigo);
    }

    private Task<HttpResponseMessage> LoginAsync(string codigo, string email, string password) =>
        _api.ClienteAnonimo().PostAsJsonAsync("/api/auth/login",
            new LoginDto { CodigoEmpresa = codigo, Email = email, Password = password }, QualiaApiFactory.Json);

    private static object Nuevo(string nombre, string email, RolEmpresa rol) =>
        new { nombre, email, password = Contrasena, rol };
}
