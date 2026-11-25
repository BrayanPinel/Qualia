using System.Net;
using Qualia.Tests.Integracion.Infraestructura;

namespace Qualia.Tests.Integracion;

/// CORS para el frontend: el origen de Vite recibe las cabeceras (también en el preflight y en los
/// ErrorDto) y un origen no configurado no las recibe.
[Collection(ColeccionPostgres.Nombre)]
public sealed class CorsIntegracionTests : IAsyncLifetime
{
    private const string OrigenFrontend = "http://localhost:5173";
    private readonly QualiaApiFactory _api;

    public CorsIntegracionTests(PostgresFixture postgres)
    {
        _api = new QualiaApiFactory(postgres, $"qualia_cors_{Guid.NewGuid():N}");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [FactConDocker]
    public async Task Preflight_desde_el_frontend_permite_post_con_authorization()
    {
        var respuesta = await PreflightAsync(OrigenFrontend);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(OrigenFrontend, Cabecera(respuesta, "Access-Control-Allow-Origin"));
        Assert.Contains("POST", Cabecera(respuesta, "Access-Control-Allow-Methods"));
        Assert.Contains("authorization", Cabecera(respuesta, "Access-Control-Allow-Headers"), StringComparison.OrdinalIgnoreCase);
    }

    [FactConDocker]
    public async Task Preflight_desde_otro_origen_no_recibe_permiso()
    {
        var respuesta = await PreflightAsync("http://sitio-ajeno.test");

        Assert.False(respuesta.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [FactConDocker]
    public async Task Error_del_backend_llega_al_frontend_con_cabecera_cors()
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/plataforma/empresas");
        peticion.Headers.Add("Origin", OrigenFrontend);

        var respuesta = await _api.ClienteAnonimo().SendAsync(peticion);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(OrigenFrontend, Cabecera(respuesta, "Access-Control-Allow-Origin"));
    }

    private Task<HttpResponseMessage> PreflightAsync(string origen)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        peticion.Headers.Add("Origin", origen);
        peticion.Headers.Add("Access-Control-Request-Method", "POST");
        peticion.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        return _api.ClienteAnonimo().SendAsync(peticion);
    }

    private static string Cabecera(HttpResponseMessage respuesta, string nombre) =>
        respuesta.Headers.TryGetValues(nombre, out var valores) ? string.Join(",", valores) : string.Empty;
}
