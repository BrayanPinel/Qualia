namespace Qualia.Infrastructure.IA;

public enum ModoProveedorIA
{
    /// Transcripción y evaluación simuladas y reproducibles.
    Simulado,

    /// Deepgram (transcripción) + Groq (actores y evaluación).
    Real,
}

/// Sección "IA" de la configuración.
public sealed class OpcionesIA
{
    public const string Seccion = "IA";

    public ModoProveedorIA Modo { get; set; } = ModoProveedorIA.Simulado;

    /// Guarda en disco las respuestas JSON crudas de Deepgram y Groq.
    public bool GuardarRespuestasCrudas { get; set; } = true;

    /// Carpeta de las respuestas crudas, relativa al directorio de la API. Está en .gitignore.
    public string DirectorioRespuestas { get; set; } = "RespuestasIA";
}

/// Sección "Deepgram". La llave va en el .env (Deepgram__ApiKey).
public sealed class OpcionesDeepgram
{
    public const string Seccion = "Deepgram";

    public string? ApiKey { get; set; }
    public string UrlBase { get; set; } = "https://api.deepgram.com/";

    public string Modelo { get; set; } = "nova-2";

    public string Idioma { get; set; } = "es";

    /// Pausa mínima en segundos para separar enunciados.
    public decimal SeparacionEnunciadosSeg { get; set; } = 0.5m;

    /// Costo de referencia en USD por minuto de audio.
    public decimal CostoPorMinutoUsd { get; set; } = 0.0043m;

    public int TiempoEsperaSeg { get; set; } = 120;
}

/// Sección "Groq". La llave va en el .env (Groq__ApiKey).
public sealed class OpcionesGroq
{
    public const string Seccion = "Groq";

    public string? ApiKey { get; set; }
    public string UrlBase { get; set; } = "https://api.groq.com/openai/v1/";

    /// Llama 3.1 70B ya no está habilitado en la cuenta; se usa gpt-oss-120b.
    public string ModeloEvaluacion { get; set; } = "openai/gpt-oss-120b";

    /// Modelo para reconocer actores (Agente / Cliente) en cada fragmento.
    public string ModeloActores { get; set; } = "openai/gpt-oss-120b";

    /// Precios estimados en USD por millón de tokens.
    public decimal CostoEntradaPorMillonUsd { get; set; } = 0.15m;

    public decimal CostoSalidaPorMillonUsd { get; set; } = 0.60m;

    public int TiempoEsperaSeg { get; set; } = 90;
}
