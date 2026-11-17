using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qualia.Application.Features.Llamadas;
using Qualia.Application.Features.Procesamiento;

namespace Qualia.Infrastructure.IA;

/// <summary>
/// Transcriptor real: Deepgram Nova-2 en español con diarización y reconocimiento de actores con Groq.
/// Envía el audio a Deepgram y guarda la respuesta JSON cruda.
/// Toma los enunciados con su hablante y tiempos.
/// Enmascara cada enunciado y pide a Groq el rol (Agente / Cliente) de cada uno.
/// Agrupa enunciados consecutivos del mismo actor en segmentos.
/// El texto devuelto no va enmascarado: el ProcesadorTrabajo lo enmascara antes de guardarlo.
/// </summary>
public sealed class TranscriptorDeepgram(
    HttpClient http,
    IOptions<OpcionesDeepgram> opciones,
    IAlmacenamientoAudio almacenamientoAudio,
    IEnmascaradorDatos enmascarador,
    IdentificadorActoresGroq identificadorActores,
    AlmacenRespuestasIA almacenRespuestas,
    ILogger<TranscriptorDeepgram> logger) : ITranscriptor
{
    private readonly OpcionesDeepgram _opciones = opciones.Value;

    public async Task<ResultadoTranscripcion> TranscribirAsync(SolicitudTranscripcion solicitud, CancellationToken ct)
    {
        var (enunciados, duracionAudio) = await TranscribirConDeepgramAsync(solicitud, ct);

        var paraActores = enunciados
            .Select(e => new EnunciadoParaActor(e.Hablante, enmascarador.Enmascarar(e.Texto)))
            .ToList();
        var (roles, uso) = await identificadorActores.IdentificarAsync(solicitud.EmpresaId, solicitud.LlamadaId, paraActores, ct);

        var hablantesDiarizados = enunciados.Select(e => e.Hablante).Distinct().Count();
        logger.LogInformation(
            "Llamada {LlamadaId}: {Enunciados} enunciados, {Hablantes} hablante(s) según diarización, actores reconocidos con Groq",
            solicitud.LlamadaId, enunciados.Count, hablantesDiarizados);

        var segmentos = AgrupadorEnunciados.Agrupar(enunciados, roles, solicitud.DuracionSeg);
        var costoDeepgram = Math.Round(duracionAudio / 60m * _opciones.CostoPorMinutoUsd, 6);

        return new ResultadoTranscripcion(segmentos, uso.TokensEntrada + uso.TokensSalida, costoDeepgram + uso.CostoUsd);
    }

    private async Task<(List<EnunciadoDeepgram> Enunciados, decimal DuracionAudio)> TranscribirConDeepgramAsync(
        SolicitudTranscripcion solicitud, CancellationToken ct)
    {
        var audio = await almacenamientoAudio.AbrirAsync(solicitud.ArchivoRuta, solicitud.DuracionSeg, ct);
        if (audio is null || audio.EsSintetico)
        {
            if (audio is not null)
            {
                await audio.Contenido.DisposeAsync();
            }

            throw new ErrorProcesamientoPermanente("No se encontró la grabación de la llamada.");
        }

        var separacion = _opciones.SeparacionEnunciadosSeg.ToString(CultureInfo.InvariantCulture);
        var url = $"v1/listen?model={_opciones.Modelo}&language={_opciones.Idioma}" +
                  $"&diarize=true&utterances=true&punctuate=true&smart_format=true&utt_split={separacion}";

        string cuerpo;
        await using (audio.Contenido)
        {
            using var contenido = new StreamContent(audio.Contenido);
            contenido.Headers.ContentType = new MediaTypeHeaderValue(audio.TipoContenido);

            using var respuesta = await http.PostAsync(url, contenido, ct);
            cuerpo = await respuesta.Content.ReadAsStringAsync(ct);
            await almacenRespuestas.GuardarAsync(solicitud.EmpresaId, solicitud.LlamadaId, "deepgram-transcripcion", cuerpo, ct);

            if (!respuesta.IsSuccessStatusCode)
            {
                throw (int)respuesta.StatusCode switch
                {
                    401 or 403 => new ErrorProcesamientoPermanente("Las credenciales de Deepgram no son válidas."),
                    402 => new ErrorProcesamientoPermanente("El crédito de Deepgram está agotado."),
                    400 or 415 => new ErrorProcesamientoPermanente("Deepgram no pudo procesar el formato del audio."),
                    429 => new ErrorProcesamiento("Deepgram alcanzó su límite de peticiones; se reintentará."),
                    _ => new ErrorProcesamiento("Deepgram no está disponible temporalmente."),
                };
            }
        }

        return Interpretar(cuerpo);
    }

    /// <summary>Extrae los enunciados y la duración de la respuesta de Deepgram.</summary>
    public static (List<EnunciadoDeepgram> Enunciados, decimal DuracionAudio) Interpretar(string json)
    {
        try
        {
            using var documento = JsonDocument.Parse(json);
            var raiz = documento.RootElement;
            var duracion = raiz.GetProperty("metadata").GetProperty("duration").GetDecimal();

            var enunciados = new List<EnunciadoDeepgram>();
            if (raiz.GetProperty("results").TryGetProperty("utterances", out var lista))
            {
                foreach (var u in lista.EnumerateArray())
                {
                    var texto = u.GetProperty("transcript").GetString()?.Trim() ?? string.Empty;
                    if (texto.Length == 0)
                    {
                        continue;
                    }

                    var hablante = u.TryGetProperty("speaker", out var s) ? s.GetInt32() : 0;
                    enunciados.Add(new EnunciadoDeepgram(hablante, u.GetProperty("start").GetDecimal(), u.GetProperty("end").GetDecimal(), texto));
                }
            }

            if (enunciados.Count == 0)
            {
                throw new ErrorProcesamientoPermanente("La transcripción no detectó voz en el audio.");
            }

            return (enunciados.OrderBy(e => e.InicioSeg).ToList(), duracion);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new ErrorProcesamiento("Deepgram devolvió una respuesta con formato inesperado.");
        }
    }
}
