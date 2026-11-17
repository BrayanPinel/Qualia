namespace Qualia.Infrastructure.Procesamiento;

/// <summary>
/// Genera semillas estables a partir de identificadores. A diferencia de GetHashCode,
/// el resultado es igual en cada ejecución, por lo que la simulación es reproducible (HU-13).
/// </summary>
internal static class SemillaDeterminista
{
    public static int Desde(params Guid[] ids)
    {
        unchecked
        {
            var hash = (int)2166136261;
            foreach (var id in ids)
            {
                foreach (var b in id.ToByteArray())
                {
                    hash = (hash ^ b) * 16777619;
                }
            }

            return hash & int.MaxValue;
        }
    }
}
