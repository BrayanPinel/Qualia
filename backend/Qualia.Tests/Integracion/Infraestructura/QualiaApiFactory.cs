using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qualia.API.Seguridad;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Features.Procesamiento;

namespace Qualia.Tests.Integracion.Infraestructura;

/// API real (Program) contra una base de datos PostgreSQL propia de cada factory.
/// El worker en segundo plano se desactiva: las pruebas ejecutan los ciclos de procesamiento
/// de forma explícita con ProcesarColaAsync para ser deterministas.
public sealed class QualiaApiFactory(
    PostgresFixture postgres,
    string baseDatos,
    int maximoIntentos = 3,
    Action<IServiceCollection>? configurarServicios = null,
    bool usarConsumoReal = false) : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public ConsumoEvaluacionRegistrado Consumo { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:PostgresConnection", postgres.CadenaConexion(baseDatos));
        builder.UseSetting("Autenticacion:ModoDemo", "true");
        builder.UseSetting("BaseDatos:AplicarEsquemaAlIniciar", "true");
        builder.UseSetting("BaseDatos:SembrarDatosDemo", "true");
        builder.UseSetting("Procesamiento:WorkerHabilitado", "false");
        builder.UseSetting("Procesamiento:MaximoIntentos", maximoIntentos.ToString());
        builder.UseSetting("Procesamiento:EsperaBaseReintento", "00:00:00");
        builder.UseSetting("Audio:ClaveFirma", "clave-de-pruebas-de-integracion-qualia-2026");
        builder.UseSetting("IA:Modo", "Simulado"); // Pruebas deterministas, sin red ni costo.

        builder.ConfigureTestServices(servicios =>
        {
            // Por defecto, doble del contrato de consumo; usarConsumoReal prueba la integración D ↔ C.
            if (!usarConsumoReal)
            {
                servicios.RemoveAll<IConsumoEvaluacion>();
                servicios.AddSingleton<IConsumoEvaluacion>(Consumo);
            }

            configurarServicios?.Invoke(servicios);
        });
    }

    public HttpClient ClienteComo(Guid usuarioId)
    {
        var cliente = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        cliente.DefaultRequestHeaders.Add(AutenticacionDemoHandler.Cabecera, usuarioId.ToString());
        return cliente;
    }

    public HttpClient ClienteAnonimo() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// Ejecuta ciclos del worker hasta vaciar la cola disponible.
    public async Task<int> ProcesarColaAsync()
    {
        var ejecutor = Services.GetRequiredService<EjecutorProcesamiento>();
        var total = 0;
        int procesados;
        do
        {
            procesados = await ejecutor.EjecutarCicloAsync(CancellationToken.None);
            total += procesados;
        }
        while (procesados > 0 && total < 100);

        return total;
    }

    public static async Task<T> LeerAsync<T>(HttpResponseMessage respuesta) =>
        (await respuesta.Content.ReadFromJsonAsync<T>(Json))!;
}
