using System.Diagnostics;
using Testcontainers.PostgreSql;

namespace Qualia.Tests.Integracion.Infraestructura;

/// <summary>Un contenedor PostgreSQL 16 compartido por todas las pruebas de integración.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _contenedor;

    public string CadenaConexion(string baseDatos)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(_contenedor!.GetConnectionString()) { Database = baseDatos };
        return builder.ConnectionString;
    }

    public async Task InitializeAsync()
    {
        if (!DockerDisponible.Valor)
        {
            return;
        }

        _contenedor = new PostgreSqlBuilder("postgres:16-alpine")
            .Build();
        await _contenedor.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_contenedor is not null)
        {
            await _contenedor.DisposeAsync();
        }
    }
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionPostgres : ICollectionFixture<PostgresFixture>
{
    public const string Nombre = "PostgreSQL";
}

/// <summary>Las pruebas de integración se omiten (no fallan) si Docker no está en ejecución.</summary>
public sealed class FactConDockerAttribute : FactAttribute
{
    public FactConDockerAttribute()
    {
        if (!DockerDisponible.Valor)
        {
            Skip = "Docker no está disponible: se omiten las pruebas de integración con PostgreSQL.";
        }
    }
}

internal static class DockerDisponible
{
    private static readonly Lazy<bool> Detectado = new(() =>
    {
        try
        {
            using var proceso = Process.Start(new ProcessStartInfo("docker", "info --format {{.ServerVersion}}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (proceso is null || !proceso.WaitForExit(20_000))
            {
                return false;
            }

            return proceso.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    });

    public static bool Valor => Detectado.Value;
}
