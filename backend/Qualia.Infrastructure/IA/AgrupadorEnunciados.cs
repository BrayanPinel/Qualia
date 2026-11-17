using Qualia.Application.Features.Procesamiento;

namespace Qualia.Infrastructure.IA;

/// <summary>Fragmento tal como lo entrega Deepgram.</summary>
public sealed record EnunciadoDeepgram(int Hablante, decimal InicioSeg, decimal FinSeg, string Texto);

/// <summary>
/// Une fragmentos consecutivos del mismo actor en un solo segmento.
/// Deepgram corta por pausas; sin agrupar, un número dictado quedaría partido
/// en varios segmentos y escaparía al enmascarado.
/// </summary>
public static class AgrupadorEnunciados
{
    public static IReadOnlyList<SegmentoTranscrito> Agrupar(
        IReadOnlyList<EnunciadoDeepgram> enunciados,
        IReadOnlyList<string> roles,
        decimal duracionLlamada,
        decimal pausaMaximaSeg = 1.5m)
    {
        if (enunciados.Count != roles.Count)
        {
            throw new ArgumentException("Debe haber un rol por enunciado.", nameof(roles));
        }

        var resultado = new List<SegmentoTranscrito>();
        string? rolActual = null;
        decimal inicio = 0, fin = 0;
        var textos = new List<string>();

        for (var i = 0; i < enunciados.Count; i++)
        {
            var e = enunciados[i];
            var continua = rolActual == roles[i] && e.InicioSeg - fin <= pausaMaximaSeg;

            if (!continua && rolActual is not null)
            {
                resultado.Add(Crear(rolActual, inicio, fin, textos, duracionLlamada));
                textos.Clear();
            }

            if (!continua)
            {
                rolActual = roles[i];
                inicio = e.InicioSeg;
            }

            fin = Math.Max(fin, e.FinSeg);
            textos.Add(e.Texto.Trim());
        }

        if (rolActual is not null)
        {
            resultado.Add(Crear(rolActual, inicio, fin, textos, duracionLlamada));
        }

        return resultado;
    }

    private static SegmentoTranscrito Crear(string rol, decimal inicio, decimal fin, List<string> textos, decimal duracion)
    {
        // Los tiempos se ajustan a la duración registrada de la llamada.
        var i = Math.Clamp(Math.Round(inicio, 2), 0, duracion);
        var f = Math.Clamp(Math.Round(fin, 2), i, duracion);
        return new SegmentoTranscrito(rol, i, f, string.Join(" ", textos.Where(t => t.Length > 0)));
    }
}
