using DotNetEnv;

namespace Qualia.API.Configuracion;

/// <summary>
/// Localiza y carga el archivo .env del proyecto API.
/// Busca, subiendo desde el directorio actual y desde la carpeta de los binarios,
/// .env, Qualia.API/.env y backend/Qualia.API/.env. Así funciona igual con
/// dotnet run, desde el IDE y desde las pruebas (WebApplicationFactory).
/// </summary>
internal static class ArchivoEnv
{
    private static readonly string[] Candidatos = [".env", Path.Combine("Qualia.API", ".env"), Path.Combine("backend", "Qualia.API", ".env")];

    public static void Cargar()
    {
        var ruta = Buscar(Directory.GetCurrentDirectory()) ?? Buscar(AppContext.BaseDirectory);
        if (ruta is not null)
        {
            Env.NoClobber().Load(ruta);
        }
    }

    private static string? Buscar(string desde)
    {
        for (var dir = new DirectoryInfo(desde); dir is not null; dir = dir.Parent)
        {
            foreach (var candidato in Candidatos)
            {
                var ruta = Path.Combine(dir.FullName, candidato);
                if (File.Exists(ruta))
                {
                    return ruta;
                }
            }
        }

        return null;
    }
}
