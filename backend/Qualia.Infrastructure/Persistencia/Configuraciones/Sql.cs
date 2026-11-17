namespace Qualia.Infrastructure.Persistencia.Configuraciones;

internal static class Sql
{
    /// <summary>Genera la condición CHECK "columna IN ('A','B',...)" a partir de un enum.</summary>
    public static string EnEnum<TEnum>(string columna) where TEnum : struct, Enum =>
        $"{columna} IN ({string.Join(", ", Enum.GetNames<TEnum>().Select(n => $"'{n}'"))})";
}
