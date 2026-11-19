using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Qualia.API.Seguridad;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Autenticacion.Dtos;
using Qualia.Application.Features.Campanas.Dtos;
using Qualia.Infrastructure.Persistence.Semillas;
using Qualia.Tests.Integracion.Infraestructura;
using Norte = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Norte;
using Sur = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Sur;

namespace Qualia.Tests.Integracion;

/// Rutas /api/empresas/{empresaId}/… escritas completas (sin el prefijo automático del cliente de pruebas):
/// la empresa de la URL debe ser la de la sesión; las rutas sin empresa ya no existen.
[Collection(ColeccionPostgres.Nombre)]
public sealed class RutasEmpresaIntegracionTests : IAsyncLifetime
{
    /// Rutas que no pertenecen a una empresa y por eso no llevan {empresaId}.
    private static readonly string[] RutasGlobales =
    [
        "api/planes", "api/empresas", "api/auth/login", "api/plataforma/auth/login",
        "api/plataforma/empresas", "api/plataforma/empresas/{empresaId:guid}/suscripcion", "api/audio-temporal/{token}",
    ];

    private readonly QualiaApiFactory _api;

    public RutasEmpresaIntegracionTests(PostgresFixture postgres)
    {
        _api = new QualiaApiFactory(postgres, $"qualia_rutas_{Guid.NewGuid():N}");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [FactConDocker]
    public void Toda_ruta_de_la_api_es_global_o_empieza_con_la_empresa()
    {
        var rutas = _api.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText!.TrimStart('/'))
            .Where(r => r.StartsWith("api/", StringComparison.Ordinal))
            .Distinct()
            .ToList();

        var sinEmpresa = rutas
            .Where(r => !r.StartsWith(RutasEmpresa.Prefijo + "/", StringComparison.Ordinal) && !RutasGlobales.Contains(r))
            .ToList();

        Assert.Empty(sinEmpresa);
        Assert.Contains(RutasEmpresa.Prefijo + "/usuarios", rutas);
        Assert.Contains(RutasEmpresa.Prefijo + "/disputas", rutas);
    }

    [FactConDocker]
    public async Task La_ruta_con_la_empresa_de_la_sesion_responde()
    {
        var norte = await ClienteJwtAsync("demo-norte", "ana.admin@norte.demo");

        var usuarios = await norte.GetAsync($"/api/empresas/{Norte.Empresa}/usuarios");
        var campanas = await norte.GetAsync($"/api/empresas/{Norte.Empresa}/campanas");

        Assert.Equal(HttpStatusCode.OK, usuarios.StatusCode);
        Assert.All((await QualiaApiFactory.LeerAsync<PaginaDto<UsuarioDto>>(usuarios)).Items, u => Assert.Equal(Norte.Empresa, u.EmpresaId));
        Assert.Equal(HttpStatusCode.OK, campanas.StatusCode);
    }

    [FactConDocker]
    public async Task Otra_empresa_en_la_url_responde_404_aunque_el_cuerpo_sea_invalido()
    {
        var norte = await ClienteJwtAsync("demo-norte", "ana.admin@norte.demo");

        HttpResponseMessage[] respuestas =
        [
            await norte.GetAsync($"/api/empresas/{Sur.Empresa}/usuarios"),
            await norte.GetAsync($"/api/empresas/{Sur.Empresa}/llamadas/{Sur.Llamadas[0]}"),
            await norte.GetAsync($"/api/empresas/{Guid.NewGuid()}/campanas"),
            await norte.PostAsJsonAsync($"/api/empresas/{Sur.Empresa}/usuarios", new { nombre = "X" }),
            // Recurso propio con la empresa ajena en la URL: tampoco pasa.
            await norte.PutAsJsonAsync($"/api/empresas/{Sur.Empresa}/campanas/{Norte.Campana}", new { nombre = "Renombrada", activa = true }),
        ];

        foreach (var r in respuestas)
        {
            Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
            Assert.Equal(RutasEmpresa.CodigoEmpresaNoEncontrada, (await QualiaApiFactory.LeerAsync<ErrorDto>(r)).Codigo);
        }
    }

    [FactConDocker]
    public async Task Sin_sesion_es_401_y_PlatformAdmin_es_403()
    {
        var anonimo = ClienteSinPrefijo();
        var plataforma = ClienteSinPrefijo();
        var sesion = await QualiaApiFactory.LeerAsync<SesionPlataformaDto>(await anonimo.PostAsJsonAsync("/api/plataforma/auth/login",
            new LoginPlataformaDto { Email = DatosDemo.Plataforma.Correo, Password = DatosDemo.Contrasena }, QualiaApiFactory.Json));
        plataforma.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.AccessToken);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync($"/api/empresas/{Norte.Empresa}/usuarios")).StatusCode);
        var comoPlataforma = await plataforma.GetAsync($"/api/empresas/{Norte.Empresa}/llamadas");
        Assert.Equal(HttpStatusCode.Forbidden, comoPlataforma.StatusCode);
        Assert.Equal("ROL_NO_AUTORIZADO", (await QualiaApiFactory.LeerAsync<ErrorDto>(comoPlataforma)).Codigo);
    }

    [FactConDocker]
    public async Task Las_rutas_sin_empresa_ya_no_existen()
    {
        var norte = await ClienteJwtAsync("demo-norte", "ana.admin@norte.demo");

        foreach (var ruta in new[] { "/api/usuarios", "/api/campanas", "/api/llamadas", "/api/disputas", "/api/suscripcion" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await norte.GetAsync(ruta)).StatusCode);
        }
    }

    [FactConDocker]
    public async Task OpenAPI_documenta_el_parametro_empresaId()
    {
        var documento = await ClienteSinPrefijo().GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/empresas/{empresaId}/usuarios", documento);
        Assert.Contains("/api/empresas/{empresaId}/disputas/{id}/resolucion", documento);
        Assert.Contains("\"name\": \"empresaId\"", documento.Replace("\"name\":\"", "\"name\": \""));
    }

    /// Cliente sin el prefijo automático: las URLs se envían tal cual.
    private HttpClient ClienteSinPrefijo() => _api.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<HttpClient> ClienteJwtAsync(string codigo, string email)
    {
        var cliente = ClienteSinPrefijo();
        var sesion = await QualiaApiFactory.LeerAsync<SesionDto>(await cliente.PostAsJsonAsync("/api/auth/login",
            new LoginDto { CodigoEmpresa = codigo, Email = email, Password = DatosDemo.Contrasena }, QualiaApiFactory.Json));
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.AccessToken);
        return cliente;
    }
}
