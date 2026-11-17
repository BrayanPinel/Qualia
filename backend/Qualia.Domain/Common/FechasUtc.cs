namespace Qualia.Domain.Common;

/// <summary>
/// Guardas para instantes UTC. Las columnas timestamptz de PostgreSQL
/// (Npgsql) solo aceptan <see cref="DateTime"/> con <see cref="DateTimeKind.Utc"/>;
/// se valida en el dominio para fallar temprano y con un mensaje claro.
/// </summary>
public static class FechasUtc
{
    public static DateTime Exigir(DateTime valor, string nombreParametro)
    {
        if (valor.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                $"La fecha '{nombreParametro}' debe estar expresada en UTC (DateTimeKind.Utc).",
                nombreParametro);
        }

        return valor;
    }
}
