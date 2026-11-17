using Qualia.Application.Features.Procesamiento;

namespace Qualia.Infrastructure.Procesamiento;

/// <summary>
/// Transcriptor del modo Simulado: genera un diálogo plausible de centro de contacto,
/// determinista para cada llamada (misma llamada ⇒ misma transcripción).
/// Incluye a propósito datos sensibles para demostrar el enmascarado.
/// Un adaptador real (p. ej. Deepgram Nova-2, fase1.md 8.1) implementaría <see cref="ITranscriptor"/>.
/// </summary>
public sealed class TranscriptorSimulado : ITranscriptor
{
    public const string HablanteAgente = "Agente";
    public const string HablanteCliente = "Cliente";

    private static readonly string[][] Guion =
    [
        [HablanteAgente, "Buenos días, gracias por llamar a Qualia Servicios, le saluda su asesor. ¿Con quién tengo el gusto?",
                         "Buenas tardes, gracias por su llamada, le saluda su asesor de turno. ¿En qué le puedo servir?"],
        [HablanteCliente, "Hola, quería consultar sobre un cobro que aparece en mi factura de este mes.",
                          "Buenas, llamo porque mi servicio de internet se cae varias veces al día."],
        [HablanteAgente, "Con gusto le ayudo. Para verificar su identidad, ¿me confirma su número de identidad y su teléfono registrado?",
                         "Claro que sí. Antes de continuar necesito verificar su identidad, ¿me indica su número de identidad por favor?"],
        [HablanteCliente, "Sí, mi identidad es 0801-1990-12345 y mi teléfono es 9876-5432.",
                          "Claro, es 0501-1985-67890, y pueden escribirme a cliente.demo@example.com."],
        [HablanteAgente, "Gracias, ya verifiqué sus datos. Estoy revisando su cuenta en el sistema.",
                         "Perfecto, la verificación fue exitosa. Permítame revisar el historial de su cuenta."],
        [HablanteCliente, "Es que el mes pasado pagué con la tarjeta 4111 1111 1111 1111 y aun así me aparece el saldo.",
                          "Ya reinicié el equipo varias veces y el problema continúa."],
        [HablanteAgente, "Entiendo su molestia. Le explico la solución: aplicaré el ajuste en su cuenta y se reflejará en su próxima factura.",
                         "Comprendo. La solución es programar una visita técnica; queda agendada para mañana por la mañana."],
        [HablanteCliente, "Perfecto, muchas gracias por la ayuda.",
                          "Está bien, espero que lo resuelvan pronto."],
        [HablanteAgente, "¿Hay algo más en lo que pueda ayudarle? Le agradezco su llamada y me despido, que tenga un excelente día.",
                         "Su caso quedó registrado. ¿Le puedo ayudar en algo adicional? Me despido agradeciendo su llamada."],
    ];

    public Task<ResultadoTranscripcion> TranscribirAsync(SolicitudTranscripcion solicitud, CancellationToken cancellationToken)
    {
        var aleatorio = new Random(SemillaDeterminista.Desde(solicitud.LlamadaId));

        var lineas = Guion
            .Select(opciones => (Hablante: opciones[0], Texto: opciones[1 + aleatorio.Next(opciones.Length - 1)]))
            // Algunas llamadas omiten la verificación o el cierre, para que las notas varíen.
            .Where((_, indice) => indice is not (2 or 8) || aleatorio.NextDouble() > 0.25)
            .ToList();

        var segmentos = DistribuirTiempos(lineas, solicitud.DuracionSeg);
        var tokens = segmentos.Sum(s => s.Texto.Length) / 4;

        return Task.FromResult(new ResultadoTranscripcion(segmentos, tokens, CostoEstimado: 0m));
    }

    /// <summary>Reparte la duración de la llamada en proporción a la longitud de cada intervención.</summary>
    private static List<SegmentoTranscrito> DistribuirTiempos(List<(string Hablante, string Texto)> lineas, decimal duracion)
    {
        var totalCaracteres = lineas.Sum(l => l.Texto.Length);
        var segmentos = new List<SegmentoTranscrito>(lineas.Count);
        var inicio = 0m;

        for (var i = 0; i < lineas.Count; i++)
        {
            var fin = i == lineas.Count - 1
                ? duracion
                : Math.Min(duracion, Math.Round(inicio + duracion * lineas[i].Texto.Length / totalCaracteres, 2));

            segmentos.Add(new SegmentoTranscrito(lineas[i].Hablante, inicio, fin, lineas[i].Texto));
            inicio = fin;
        }

        return segmentos;
    }
}
