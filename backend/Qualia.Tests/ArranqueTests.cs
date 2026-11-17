using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Qualia.Tests;

/// <summary>
/// Prueba mínima de bootstrap: la API debe iniciar y exponer OpenAPI
/// sin necesidad de una base de datos levantada.
/// </summary>
public class ArranqueTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ArranqueTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LaApiArrancaYExponeOpenApi()
    {
        using var client = _factory
            .WithWebHostBuilder(builder => builder
                .UseEnvironment("Development")
                // Sin base de datos: no se aplica el esquema, no se siembra ni se inicia el worker.
                .UseSetting("BaseDatos:AplicarEsquemaAlIniciar", "false")
                .UseSetting("BaseDatos:SembrarDatosDemo", "false")
                .UseSetting("Procesamiento:WorkerHabilitado", "false"))
            .CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var contenido = await response.Content.ReadAsStringAsync();
        Assert.Contains("openapi", contenido, StringComparison.OrdinalIgnoreCase);
    }
}
