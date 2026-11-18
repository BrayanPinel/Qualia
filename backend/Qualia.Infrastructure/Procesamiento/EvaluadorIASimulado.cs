using System.Globalization;
using System.Text;
using Qualia.Application.Features.Procesamiento;
using Qualia.Domain.Enums;

namespace Qualia.Infrastructure.Procesamiento;

/// <summary>
/// Evaluador del modo Simulado: busca en la transcripción enmascarada el segmento del agente
/// que mejor coincide con las palabras clave de cada criterio, cita un fragmento literal de él
/// y asigna un puntaje determinista. Si no hay coincidencias, informa ausencia de evidencia.
/// El adaptador real es EvaluadorGroq; ambos pasan por la misma validación de evidencias.
/// </summary>
public sealed class EvaluadorIASimulado : IEvaluadorIA
{
    public const string Modelo = "qualia-simulador-v1";

    private const int PalabrasPorCita = 12;

    public Task<ResultadoEvaluacionIA> EvaluarAsync(SolicitudEvaluacion solicitud, CancellationToken cancellationToken)
    {
        var resultados = new List<CriterioEvaluado>(solicitud.Criterios.Count);

        foreach (var criterio in solicitud.Criterios)
        {
            var aleatorio = new Random(SemillaDeterminista.Desde(solicitud.LlamadaId, criterio.CriterioId));
            var claves = PalabrasClave($"{criterio.Nombre} {criterio.Descripcion}");

            var mejor = solicitud.Segmentos
                .Where(s => s.Hablante == TranscriptorSimulado.HablanteAgente)
                .Select(s => (Segmento: s, Coincidencias: Coincidencias(claves, s.Texto)))
                .Where(x => x.Coincidencias > 0)
                .OrderByDescending(x => x.Coincidencias)
                .ThenBy(x => x.Segmento.Orden)
                .FirstOrDefault();

            if (mejor.Segmento is not null)
            {
                var puntaje = aleatorio.Next(70, 101);
                resultados.Add(new CriterioEvaluado(
                    criterio.CriterioId,
                    puntaje,
                    $"El agente aborda \"{criterio.Nombre}\" en la intervención {mejor.Segmento.Orden}.",
                    [new EvidenciaPropuesta(mejor.Segmento.SegmentoId, Citar(mejor.Segmento.Texto))]));
            }
            else
            {
                var puntaje = aleatorio.Next(20, 60);
                var cierre = solicitud.Segmentos.LastOrDefault(s => s.Hablante == TranscriptorSimulado.HablanteAgente);

                // Falta: la mitad de las veces se cita la intervención final del agente, donde debió cumplirlo;
                // en las demás no hay fragmento y la evaluación pasa a revisión del supervisor.
                if (cierre is not null && aleatorio.Next(2) == 0)
                {
                    resultados.Add(new CriterioEvaluado(
                        criterio.CriterioId,
                        puntaje,
                        $"El agente cierra en la intervención {cierre.Orden} sin cumplir \"{criterio.Nombre}\".",
                        [new EvidenciaPropuesta(cierre.SegmentoId, Citar(cierre.Texto))]));
                }
                else
                {
                    resultados.Add(new CriterioEvaluado(
                        criterio.CriterioId,
                        puntaje,
                        $"No se identificó en la conversación una intervención que cumpla \"{criterio.Nombre}\".",
                        []));
                }
            }
        }

        var caracteresEntrada = solicitud.Segmentos.Sum(s => s.Texto.Length) + solicitud.Criterios.Sum(c => c.Descripcion.Length);
        var caracteresSalida = resultados.Sum(r => r.Justificacion.Length + r.Evidencias.Sum(e => e.CitaTextual.Length));

        return Task.FromResult(new ResultadoEvaluacionIA(
            Modelo, ModoIA.Simulado, resultados, caracteresEntrada / 4, caracteresSalida / 4, CostoEstimado: 0m));
    }

    /// <summary>Primeras palabras del segmento: siempre es un fragmento literal del texto.</summary>
    private static string Citar(string texto)
    {
        var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return palabras.Length <= PalabrasPorCita ? texto : string.Join(' ', palabras.Take(PalabrasPorCita));
    }

    private static HashSet<string> PalabrasClave(string texto) =>
        Normalizar(texto)
            .Split([' ', ',', '.', ';', ':', '¿', '?', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length >= 5)
            .Select(Raiz)
            .ToHashSet();

    private static int Coincidencias(HashSet<string> claves, string texto) =>
        PalabrasClave(texto).Count(claves.Contains);

    /// <summary>Raíz simple: primeros 5 caracteres ("verificar" ≈ "verifiqué", "saludo" ≈ "saluda", "ayuda" ≈ "ayudarle").</summary>
    private static string Raiz(string palabra) => palabra.Length <= 5 ? palabra : palabra[..5];

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
