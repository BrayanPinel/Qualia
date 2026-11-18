using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Qualia.Application.Features.Procesamiento;
using Qualia.Domain.Enums;

namespace Qualia.Infrastructure.IA;

/// <summary>
/// Evaluador real: pide a Groq un borrador por criterio con evidencia citada.
/// Los criterios se envían con ids cortos (C1, C2…) para que el modelo no altere los GUID.
/// Los segmentos se identifican por su número de orden; las citas se validan después
/// en <see cref="ConstructorEvaluacion"/> (RN-09).
/// El texto enviado ya está enmascarado (RN-14).
/// </summary>
public sealed class EvaluadorGroq(ClienteGroq groq, IOptions<OpcionesGroq> opciones) : IEvaluadorIA
{
    private static readonly JsonSerializerOptions JsonRubrica = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private const string Instrucciones =
        "Eres un auditor de calidad de un centro de contacto. Evalúas SOLO el desempeño del Agente en la llamada, criterio por criterio, según la rúbrica.\n" +
        "Reglas:\n" +
        "1. Devuelve exactamente un resultado por cada criterio de la rúbrica, usando su id.\n" +
        "2. puntaje: número entero de 0 a 100 (100 = cumple plenamente el criterio).\n" +
        "3. justificacion: en español, máximo dos oraciones, basada únicamente en la transcripción.\n" +
        "4. evidencias: fragmentos que sustentan la nota. 'segmento' es el número entre corchetes y 'cita' debe copiarse LITERALMENTE " +
        "de ese segmento (máximo 25 palabras, sin corregir ni resumir, incluidos marcadores como [IDENTIDAD]).\n" +
        "5. Si el agente NO cumple el criterio (falta), las evidencias deben ser el fragmento donde comete la falta " +
        "(lo que dijo mal, o la intervención en la que debió cumplirlo y no lo hizo), para que el supervisor lo escuche.\n" +
        "6. Si no hay evidencia en la transcripción, devuelve evidencias vacías y explícalo en la justificación. No inventes.\n" +
        "7. Los datos sensibles aparecen enmascarados entre corchetes; no intentes deducirlos.\n" +
        "Responde SOLO con JSON: {\"criterios\":[{\"id\":\"C1\",\"puntaje\":0,\"justificacion\":\"\",\"evidencias\":[{\"segmento\":1,\"cita\":\"\"}]}]}";

    public async Task<ResultadoEvaluacionIA> EvaluarAsync(SolicitudEvaluacion solicitud, CancellationToken ct)
    {
        var idsCortos = solicitud.Criterios
            .Select((c, i) => (Corto: $"C{i + 1}", c.CriterioId))
            .ToDictionary(x => x.Corto, x => x.CriterioId);
        var segmentosPorOrden = solicitud.Segmentos.ToDictionary(s => s.Orden, s => s.SegmentoId);

        var rubrica = solicitud.Criterios.Select((c, i) => new
        {
            id = $"C{i + 1}",
            nombre = c.Nombre,
            descripcion = c.Descripcion,
            peso = c.Peso,
            esCritico = c.EsCritico,
            umbralCumplimiento = c.UmbralCumplimiento,
        });

        var contenido = new StringBuilder()
            .AppendLine("RÚBRICA:")
            .AppendLine(JsonSerializer.Serialize(rubrica, JsonRubrica))
            .AppendLine()
            .AppendLine("TRANSCRIPCIÓN ([segmento] Hablante: texto):");
        foreach (var s in solicitud.Segmentos.OrderBy(s => s.Orden))
        {
            contenido.Append('[').Append(s.Orden).Append("] ").Append(s.Hablante).Append(": ").AppendLine(s.Texto);
        }

        var modelo = opciones.Value.ModeloEvaluacion;
        var respuesta = await groq.CompletarJsonAsync(
            solicitud.EmpresaId, solicitud.LlamadaId, "evaluacion", modelo, Instrucciones, contenido.ToString(), ct);

        var criterios = Interpretar(respuesta.Contenido, idsCortos, segmentosPorOrden);
        return new ResultadoEvaluacionIA(modelo, ModoIA.Real, criterios, respuesta.TokensEntrada, respuesta.TokensSalida, respuesta.CostoUsd);
    }

    /// <summary>
    /// Traduce la respuesta del modelo. Un criterio desconocido o un segmento inexistente
    /// se dejan pasar marcados para que <see cref="ConstructorEvaluacion"/> los rechace.
    /// </summary>
    public static IReadOnlyList<CriterioEvaluado> Interpretar(
        string json, IReadOnlyDictionary<string, Guid> idsCortos, IReadOnlyDictionary<int, Guid> segmentosPorOrden)
    {
        try
        {
            using var documento = JsonDocument.Parse(json);
            var resultado = new List<CriterioEvaluado>();

            foreach (var c in documento.RootElement.GetProperty("criterios").EnumerateArray())
            {
                var id = c.GetProperty("id").GetString()?.Trim().ToUpperInvariant() ?? string.Empty;
                var criterioId = idsCortos.TryGetValue(id, out var guid) ? guid : Guid.NewGuid();

                var evidencias = new List<EvidenciaPropuesta>();
                if (c.TryGetProperty("evidencias", out var lista) && lista.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in lista.EnumerateArray())
                    {
                        var orden = LeerEntero(e.GetProperty("segmento"));
                        var segmentoId = segmentosPorOrden.TryGetValue(orden, out var s) ? s : Guid.Empty;
                        evidencias.Add(new EvidenciaPropuesta(segmentoId, e.GetProperty("cita").GetString() ?? string.Empty));
                    }
                }

                resultado.Add(new CriterioEvaluado(
                    criterioId,
                    LeerDecimal(c.GetProperty("puntaje")),
                    c.TryGetProperty("justificacion", out var j) ? j.GetString() ?? string.Empty : string.Empty,
                    evidencias));
            }

            return resultado;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new ErrorProcesamiento("El evaluador de IA devolvió un formato inesperado.");
        }
    }

    private static decimal LeerDecimal(JsonElement e) => e.ValueKind == JsonValueKind.String
        ? decimal.Parse(e.GetString()!, CultureInfo.InvariantCulture)
        : e.GetDecimal();

    private static int LeerEntero(JsonElement e) => e.ValueKind == JsonValueKind.String
        ? int.Parse(e.GetString()!, CultureInfo.InvariantCulture)
        : e.GetInt32();
}
