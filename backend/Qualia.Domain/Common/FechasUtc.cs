namespace Qualia.Domain.Common;

/// Guardas para instantes UTC. Las columnas timestamptz de PostgreSQL
/// (Npgsql) solo aceptan DateTime con DateTimeKind.Utc;
/// se valida en el dominio para fallar temprano y con un mensaje claro.
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
