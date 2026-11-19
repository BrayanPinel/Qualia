namespace Qualia.Infrastructure.Persistence.Configurations;

internal static class Sql
{
    /// Genera la condición CHECK "columna IN ('A','B',...)" a partir de un enum.
    public static string EnEnum<TEnum>(string columna) where TEnum : struct, Enum =>
        $"{columna} IN ({string.Join(", ", Enum.GetNames<TEnum>().Select(n => $"'{n}'"))})";
}
