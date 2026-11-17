using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qualia.API.Seguridad;
using Qualia.Application.Comun.Contratos;
using Qualia.Application.Features.Procesamiento;

namespace Qualia.Tests.Integracion.Infraestructura;

/// <summary>
/// API real (Program) contra una base de datos PostgreSQL propia de cada factory.
/// El worker en segundo plano se desactiva: las pruebas ejecutan los ciclos de procesamiento
/// de forma explícita con <see cref="ProcesarColaAsync"/> para ser deterministas.
/// </summary>
public sealed class QualiaApiFactory(
    PostgresFixture postgres,
    string baseDatos,
    int maximoIntentos = 3,
    Action<IServiceCollection>? configurarServicios = null) : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public ConsumoEvaluacionRegistrado Consumo { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", postgres.CadenaConexion(baseDatos));
        builder.UseSetting("Autenticacion:ModoDemo", "true");
        builder.UseSetting("BaseDatos:AplicarEsquemaAlIniciar", "true");
        builder.UseSetting("BaseDatos:SembrarDatosDemo", "true");
        builder.UseSetting("Procesamiento:WorkerHabilitado", "false");
        builder.UseSetting("Procesamiento:MaximoIntentos", maximoIntentos.ToString());
        builder.UseSetting("Procesamiento:EsperaBaseReintento", "00:00:00");
        builder.UseSetting("Audio:ClaveFirma", "clave-de-pruebas-de-integracion-qualia-2026");

        builder.ConfigureTestServices(servicios =>
        {
            servicios.RemoveAll<IConsumoEvaluacion>();
            servicios.AddSingleton<IConsumoEvaluacion>(Consumo);
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

    /// <summary>Ejecuta ciclos del worker hasta vaciar la cola disponible.</summary>
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
