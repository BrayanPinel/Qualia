namespace Qualia.API.Configuracion;

/// Política CORS para el frontend (React/Vite), que corre en otro origen.
/// Los orígenes permitidos se leen de CORS_ALLOWED_ORIGINS, separados por comas
/// (por ejemplo "http://localhost:5173,https://qualia.vercel.app"). Sin la variable, en Development
/// se permite el servidor de Vite y fuera de Development no se permite ningún origen (falla cerrada).
internal static class PoliticaCors
{
    public const string Nombre = "Frontend";
    public const string Variable = "CORS_ALLOWED_ORIGINS";
    public const string OrigenViteLocal = "http://localhost:5173";

    public static IServiceCollection AgregarCorsQualia(this IServiceCollection services, IConfiguration configuracion, IHostEnvironment entorno)
    {
        var origenes = LeerOrigenes(configuracion[Variable]);
        if (origenes.Length == 0 && entorno.IsDevelopment())
        {
            origenes = [OrigenViteLocal];
        }

        services.AddCors(o => o.AddPolicy(Nombre, politica =>
        {
            // Sin cookies: el token viaja en la cabecera Authorization, así que no hace falta AllowCredentials.
            politica.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod();
        }));

        return services;
    }

    /// Separa la lista, quita espacios y la barra final ("http://x/" y "http://x" son el mismo origen).
    internal static string[] LeerOrigenes(string? valor) =>
        (valor ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(o => o.TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
