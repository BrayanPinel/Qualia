using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Infrastructure.Multitenancy;
using Qualia.Infrastructure.Persistence;
using Qualia.Infrastructure.Persistence.Repositories;
using Qualia.Infrastructure.Persistence.Seeders;

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

        // TryAddScoped permite que Rol A registre su implementación de IContextoEmpresa sin conflicto.
        services.TryAddScoped<ContextoEmpresaAmbiente>();
        services.TryAddScoped<IContextoEmpresa>(sp => sp.GetRequiredService<ContextoEmpresaAmbiente>());

        services.AddScoped<IRepositorioSuscripciones, RepositorioSuscripciones>();
        services.AddScoped<PlanesSeeder>();

        return services;
    }
}
