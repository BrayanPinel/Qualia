using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qualia.Infrastructure.Persistence;

namespace Qualia.Infrastructure;
/*
 <summary>
 Composición de infraestructura. La cadena de conexión se valida al
 resolver el DbContext, no al arrancar la API, para que el bootstrap
 funcione sin una base de datos levantada.
 </summary>
 */
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgresConnection");

        services.AddDbContext<QualiaDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Falta la cadena de conexión 'ConnectionStrings:PostgresConnection'" +
                    ".");
            }

            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(QualiaDbContext).Assembly.FullName));
        });

        return services;
    }
}
