using System.Text.RegularExpressions;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;
using Qualia.Domain.Servicios;

namespace Qualia.Application.Features.Procesamiento;

/// <summary>
/// Valida la respuesta del evaluador y construye la Evaluacion_IA con sus detalles y evidencias.
/// Debe evaluar cada criterio de la rúbrica de la llamada exactamente una vez (si no: error, se reintenta).
/// Puntajes entre 0 y 100 (RN-05). El cumplimiento se calcula en el servidor (RN-08).
/// Cada evidencia debe referirse a un segmento de LA MISMA llamada y su cita debe existir en ese segmento (RN-09).
/// Las evidencias inválidas se descartan y la evaluación queda con requiere_revision = true (RN-10).
/// Sin evidencias válidas, el detalle queda sin filas de evidencia y la observación lo indica explícitamente.
/// </summary>

public static partial class ConstructorEvaluacion
{
    public const int LongitudMaximaCita = 1000;
    public const string NotaSinEvidencia = "Sin evidencia en la transcripción.";

    public sealed record Resultado(EvaluacionIA Evaluacion, int EvidenciasDescartadas);

    public static Resultado Construir(
        Guid empresaId,
        Guid llamadaId,
        ResultadoEvaluacionIA respuesta,
        IReadOnlyCollection<CriterioRubrica> criteriosRubrica,
        IReadOnlyCollection<SegmentoTranscripcion> segmentosLlamada,
        DateTime ahoraUtc)
    {
        ValidarCobertura(respuesta, criteriosRubrica);

        var segmentos = segmentosLlamada
            .Where(s => s.EmpresaId == empresaId && s.LlamadaId == llamadaId)
            .ToDictionary(s => s.Id);

        var evaluacion = new EvaluacionIA
        {
            EmpresaId = empresaId,
            LlamadaId = llamadaId,
            ModeloUsado = Recortar(respuesta.ModeloUsado, 100),
            ModoIA = respuesta.Modo,
            Estado = EstadoEvaluacion.EvaluadaPorIA,
            FechaEvaluacion = ahoraUtc,
            Revision = 1,
        };

        var descartadas = 0;
        var criteriosPorId = criteriosRubrica.ToDictionary(c => c.Id);

        foreach (var resultadoCriterio in respuesta.Criterios)
        {
            var criterio = criteriosPorId[resultadoCriterio.CriterioId];
            var puntaje = Math.Round(resultadoCriterio.Puntaje, 2, MidpointRounding.AwayFromZero);

            var detalle = new DetalleEvaluacion
            {
                EmpresaId = empresaId,
                EvaluacionId = evaluacion.Id,
                CriterioId = criterio.Id,
                PuntajeCriterio = puntaje,
                CumpleCriterio = CalculadoraPuntaje.Cumple(puntaje, criterio.UmbralCumplimiento),
            };

            var descartadasCriterio = 0;
            foreach (var propuesta in resultadoCriterio.Evidencias)
            {
                var cita = propuesta.CitaTextual?.Trim() ?? string.Empty;
                var esValida = segmentos.TryGetValue(propuesta.SegmentoId, out var segmento)
                               && EsCitaValida(cita, segmento.Texto);

                if (!esValida)
                {
                    descartadasCriterio++;
                    continue;
                }

                // PK (empresa, detalle, segmento): una sola cita por segmento y criterio.
                if (detalle.Evidencias.Any(e => e.SegmentoId == propuesta.SegmentoId))
                {
                    continue;
                }

                detalle.Evidencias.Add(new EvidenciaCriterio
                {
                    EmpresaId = empresaId,
                    DetalleId = detalle.Id,
                    SegmentoId = propuesta.SegmentoId,
                    CitaTextual = cita,
                });
            }

            detalle.Observacion = ComponerObservacion(resultadoCriterio.Justificacion, detalle.Evidencias.Count, descartadasCriterio);
            descartadas += descartadasCriterio;
            evaluacion.Detalles.Add(detalle);
        }

        evaluacion.RequiereRevision = descartadas > 0;
        return new Resultado(evaluacion, descartadas);
    }

    /// <summary>
    /// La cita debe aparecer literalmente en el texto (enmascarado) del segmento.
    /// Se toleran diferencias de mayúsculas y de espacios.
    /// </summary>

    public static bool EsCitaValida(string cita, string textoSegmento)
    {
        if (string.IsNullOrWhiteSpace(cita) || cita.Length > LongitudMaximaCita)
        {
            return false;
        }

        return Normalizar(textoSegmento).Contains(Normalizar(cita), StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidarCobertura(ResultadoEvaluacionIA respuesta, IReadOnlyCollection<CriterioRubrica> criterios)
    {
        var esperados = criterios.Select(c => c.Id).ToHashSet();
        var recibidos = respuesta.Criterios.Select(c => c.CriterioId).ToList();

        if (recibidos.Count != recibidos.Distinct().Count())
        {
            throw new ErrorProcesamiento("El evaluador devolvió criterios duplicados.");
        }

        if (!esperados.SetEquals(recibidos))
        {
            throw new ErrorProcesamiento("El evaluador no devolvió exactamente los criterios de la rúbrica de la llamada.");
        }

        if (respuesta.Criterios.Any(c => c.Puntaje is < 0 or > 100))
        {
            throw new ErrorProcesamiento("El evaluador devolvió un puntaje fuera del rango 0 a 100.");
        }
    }

    private static string ComponerObservacion(string justificacion, int evidenciasValidas, int descartadas)
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(justificacion))
        {
            partes.Add(justificacion.Trim());
        }

        if (evidenciasValidas == 0)
        {
            partes.Add(NotaSinEvidencia);
        }

        if (descartadas > 0)
        {
            partes.Add($"Se descartaron {descartadas} evidencia(s) inválida(s); requiere revisión humana.");
        }

        return Recortar(string.Join(" ", partes), 2000);
    }

    private static string Normalizar(string texto) => EspaciosRegex().Replace(texto.Trim(), " ");

    private static string Recortar(string texto, int maximo) => texto.Length <= maximo ? texto : texto[..maximo];

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosRegex();
}
