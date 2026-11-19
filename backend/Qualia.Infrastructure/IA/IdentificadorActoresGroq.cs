using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Qualia.Application.Features.Procesamiento;

namespace Qualia.Infrastructure.IA;

/// Hablante: Etiqueta de diarización de Deepgram (0, 1…). Es solo una pista.
/// Texto: Texto ya enmascarado (RN-14).
public sealed record EnunciadoParaActor(int Hablante, string Texto);

/// Reconoce el actor (Agente / Cliente) de cada fragmento de la llamada usando Groq.
/// La diarización de Deepgram separa voces, pero no sabe quién es el agente y con voces
/// parecidas puede asignar toda la llamada a un solo hablante; Groq decide por el contenido.
public sealed class IdentificadorActoresGroq(ClienteGroq groq, IOptions<OpcionesGroq> opciones)
{
    public const string Agente = "Agente";
    public const string Cliente = "Cliente";

    private const string Instrucciones =
        "Eres un analista de calidad de un centro de contacto. Recibes, en orden, los fragmentos transcritos de una llamada telefónica. " +
        "La diarización automática (etiqueta 'hablante') puede estar equivocada: úsala solo como pista. " +
        "Determina por el contenido de la conversación quién dice cada fragmento: 'Agente' (empleado de la empresa que atiende o realiza la llamada) " +
        "o 'Cliente' (la persona atendida). Los datos sensibles aparecen enmascarados entre corchetes. " +
        "Responde SOLO un objeto JSON con TODOS los fragmentos: {\"fragmentos\":[{\"n\":1,\"rol\":\"Agente\"}]}";

    public async Task<(IReadOnlyList<string> Roles, RespuestaGroq Uso)> IdentificarAsync(
        Guid empresaId, Guid llamadaId, IReadOnlyList<EnunciadoParaActor> enunciados, CancellationToken ct)
    {
        var lineas = new StringBuilder();
        for (var i = 0; i < enunciados.Count; i++)
        {
            lineas.Append(i + 1).Append(". [hablante ").Append(enunciados[i].Hablante).Append("] ").AppendLine(enunciados[i].Texto);
        }

        var respuesta = await groq.CompletarJsonAsync(
            empresaId, llamadaId, "actores", opciones.Value.ModeloActores, Instrucciones, lineas.ToString(), ct);

        return (Interpretar(respuesta.Contenido, enunciados.Count), respuesta);
    }

    /// Exige un rol válido para cada fragmento, exactamente una vez.
    public static IReadOnlyList<string> Interpretar(string json, int cantidad)
    {
        var roles = new string?[cantidad];
        try
        {
            using var documento = JsonDocument.Parse(json);
            foreach (var fragmento in documento.RootElement.GetProperty("fragmentos").EnumerateArray())
            {
                var n = fragmento.GetProperty("n").GetInt32();
                var rol = fragmento.GetProperty("rol").GetString()?.Trim();
                if (n < 1 || n > cantidad || roles[n - 1] is not null)
                {
                    throw new ErrorProcesamiento("El reconocimiento de actores devolvió fragmentos inválidos.");
                }

                roles[n - 1] = rol?.ToLowerInvariant() switch
                {
                    "agente" => Agente,
                    "cliente" => Cliente,
                    _ => throw new ErrorProcesamiento("El reconocimiento de actores devolvió un rol desconocido."),
                };
            }
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new ErrorProcesamiento("El reconocimiento de actores devolvió un formato inesperado.");
        }

        if (roles.Any(r => r is null))
        {
            throw new ErrorProcesamiento("El reconocimiento de actores no clasificó todos los fragmentos.");
        }

        return roles!;
    }
}
