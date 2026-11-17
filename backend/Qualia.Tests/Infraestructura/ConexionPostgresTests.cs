using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qualia.Infrastructure.Persistence;

namespace Qualia.Tests.Infraestructura;

/// <summary>
/// Verificación de integración contra PostgreSQL real usando el
/// <see cref="QualiaDbContext"/> existente.
/// No crea entidades, no genera migraciones y no modifica el modelo de datos:
/// solo comprueba que la API carga su configuración y que el contexto puede
/// abrir una conexión.
/// Requiere el contenedor de infra/docker-compose.yml en ejecución.
/// </summary>
public sealed class ConexionPostgresTests
{
    private const string ClaveCadena = "PostgresConnection";
    private const string VariableEntorno = "ConnectionStrings__PostgresConnection";

    [Fact]
    public void LaApiCargaLaCadenaDeConexionDesdeSuEnv()
    {
        using var factory = new WebApplicationFactory<Program>();

        // appsettings.json deja "PostgresConnection" vacía a propósito, así que
        // un valor no vacío solo puede venir del .env del proyecto API.
        var configuracion = factory.Services.GetRequiredService<IConfiguration>();
        var cadena = configuracion.GetConnectionString(ClaveCadena);

        Assert.False(
            string.IsNullOrWhiteSpace(cadena),
            "La API no resolvió la cadena de conexión desde backend/Qualia.API/.env.");

        Assert.False(
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(VariableEntorno)),
            $"La variable de entorno '{VariableEntorno}' no quedó cargada en el proceso.");
    }

    [Fact]
    public async Task QualiaDbContextPuedeConectarConPostgres()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var alcance = factory.Services.CreateScope();

        var contexto = alcance.ServiceProvider.GetRequiredService<QualiaDbContext>();

        Assert.NotNull(contexto.Database.ProviderName);
        Assert.Contains(
            "Npgsql",
            contexto.Database.ProviderName,
            StringComparison.OrdinalIgnoreCase);

        var conectado = await contexto.Database.CanConnectAsync();

        Assert.True(
            conectado,
            "El QualiaDbContext no pudo conectar con PostgreSQL. " +
            "Verifica que el contenedor de infra/docker-compose.yml esté en ejecución.");
    }
}
