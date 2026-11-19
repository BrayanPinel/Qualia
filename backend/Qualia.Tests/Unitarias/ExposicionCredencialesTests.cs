using System.Reflection;
using Qualia.Application.Common.Seguridad;

namespace Qualia.Tests.Unitarias;

/// Guarda: ningún tipo público de Application ni de la API (DTOs, respuestas, comandos)
/// puede exponer un hash de contraseña. Solo las entidades de dominio lo tienen.
public sealed class ExposicionCredencialesTests
{
    private static readonly string[] NombresProhibidos = ["PasswordHash", "HashContrasena", "ContrasenaHash"];

    public static TheoryData<string> Ensamblados => new()
    {
        typeof(IServicioHashContrasena).Assembly.GetName().Name!,
        typeof(Program).Assembly.GetName().Name!,
    };

    [Theory]
    [MemberData(nameof(Ensamblados))]
    public void Ningun_tipo_publico_expone_un_hash_de_contrasena(string ensamblado)
    {
        var expuestos = Assembly.Load(ensamblado).GetExportedTypes()
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(p => NombresProhibidos.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .Select(p => $"{t.FullName}.{p.Name}"))
            .ToList();

        Assert.Empty(expuestos);
    }
}
