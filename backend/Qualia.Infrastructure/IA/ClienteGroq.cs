using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Qualia.Application.Features.Procesamiento;

namespace Qualia.Infrastructure.IA;

public sealed record RespuestaGroq(string Contenido, int TokensEntrada, int TokensSalida, decimal CostoUsd);

/// Cliente HTTP de Groq en modo JSON.
/// Guarda la solicitud y la respuesta crudas, y traduce los códigos HTTP a errores del motor:
/// 429 y 5xx se reintentan; credenciales inválidas fallan de inmediato.
public sealed class ClienteGroq(HttpClient http, IOptions<OpcionesGroq> opciones, AlmacenRespuestasIA almacen)
{
    private readonly OpcionesGroq _opciones = opciones.Value;

    public async Task<RespuestaGroq> CompletarJsonAsync(
        Guid empresaId,
        Guid llamadaId,
        string etapa,
        string modelo,
        string instruccionesSistema,
        string contenidoUsuario,
        CancellationToken ct)
    {
        var solicitud = new JsonObject
        {
            ["model"] = modelo,
            ["temperature"] = 0,
            ["response_format"] = new JsonObject { ["type"] = "json_object" },
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = instruccionesSistema },
                new JsonObject { ["role"] = "user", ["content"] = contenidoUsuario },
            },
        }.ToJsonString();

        await almacen.GuardarAsync(empresaId, llamadaId, $"groq-{etapa}-solicitud", solicitud, ct);

        string cuerpo;
        for (var intento = 1; ; intento++)
        {
            using var respuesta = await http.PostAsync("chat/completions", new StringContent(solicitud, Encoding.UTF8, "application/json"), ct);
            cuerpo = await respuesta.Content.ReadAsStringAsync(ct);
            await almacen.GuardarAsync(empresaId, llamadaId, $"groq-{etapa}-respuesta", cuerpo, ct);

            if (respuesta.IsSuccessStatusCode)
            {
                break;
            }

            // Ante 429 se espera lo que indica Groq y se vuelve a intentar.
            // Si sigue limitado, el worker reintenta el trabajo más tarde.
            if (respuesta.StatusCode == HttpStatusCode.TooManyRequests && intento < MaximoEsperasPorLimite)
            {
                await Task.Delay(EsperaPorLimite(respuesta), ct);
                continue;
            }

            throw TraducirError(respuesta.StatusCode);
        }

        try
        {
            using var documento = JsonDocument.Parse(cuerpo);
            var raiz = documento.RootElement;
            var contenido = raiz.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(contenido))
            {
                throw new ErrorProcesamiento("Groq devolvió una respuesta vacía.");
            }

            var entrada = raiz.TryGetProperty("usage", out var uso) ? uso.GetProperty("prompt_tokens").GetInt32() : 0;
            var salida = raiz.TryGetProperty("usage", out uso) ? uso.GetProperty("completion_tokens").GetInt32() : 0;
            var costo = Math.Round(
                entrada * _opciones.CostoEntradaPorMillonUsd / 1_000_000m + salida * _opciones.CostoSalidaPorMillonUsd / 1_000_000m, 6);

            return new RespuestaGroq(contenido, entrada, salida, costo);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            throw new ErrorProcesamiento("Groq devolvió una respuesta con formato inesperado.");
        }
    }

    private const int MaximoEsperasPorLimite = 3;
    private static readonly TimeSpan EsperaMaximaPorLimite = TimeSpan.FromSeconds(30);

    /// Usa la cabecera retry-after de Groq; por defecto 5 s y como máximo 30 s.
    private static TimeSpan EsperaPorLimite(HttpResponseMessage respuesta)
    {
        var sugerida = respuesta.Headers.RetryAfter?.Delta
                       ?? (respuesta.Headers.TryGetValues("retry-after", out var valores)
                           && double.TryParse(valores.FirstOrDefault(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s)
                               ? TimeSpan.FromSeconds(s)
                               : TimeSpan.FromSeconds(5));
        return sugerida > EsperaMaximaPorLimite ? EsperaMaximaPorLimite : sugerida + TimeSpan.FromMilliseconds(500);
    }

    private static ErrorProcesamiento TraducirError(HttpStatusCode estado) => (int)estado switch
    {
        401 or 403 => new ErrorProcesamientoPermanente("Las credenciales de Groq no son válidas."),
        404 => new ErrorProcesamientoPermanente("El modelo de Groq configurado no está disponible."),
        429 => new ErrorProcesamiento("Groq alcanzó su límite de peticiones; se reintentará."),
        >= 500 => new ErrorProcesamiento("Groq no está disponible temporalmente."),
        _ => new ErrorProcesamiento($"Groq rechazó la solicitud (HTTP {(int)estado})."),
    };
}
