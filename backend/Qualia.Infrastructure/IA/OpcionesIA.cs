namespace Qualia.Infrastructure.IA;

public enum ModoProveedorIA
{
    /// <summary>Transcripción y evaluación simuladas y reproducibles.</summary>
    Simulado,

    /// <summary>Deepgram (transcripción) + Groq (actores y evaluación).</summary>
    Real,
}

/// <summary>Sección "IA" de la configuración.</summary>
public sealed class OpcionesIA
{
    public const string Seccion = "IA";

    public ModoProveedorIA Modo { get; set; } = ModoProveedorIA.Simulado;

    /// <summary>Guarda en disco las respuestas JSON crudas de Deepgram y Groq.</summary>
    public bool GuardarRespuestasCrudas { get; set; } = true;

    /// <summary>Carpeta de las respuestas crudas, relativa al directorio de la API. Está en .gitignore.</summary>
    public string DirectorioRespuestas { get; set; } = "RespuestasIA";
}

/// <summary>Sección "Deepgram". La llave va en el .env (Deepgram__ApiKey).</summary>
public sealed class OpcionesDeepgram
{
    public const string Seccion = "Deepgram";

    public string? ApiKey { get; set; }
    public string UrlBase { get; set; } = "https://api.deepgram.com/";

    public string Modelo { get; set; } = "nova-2";

    public string Idioma { get; set; } = "es";

    /// <summary>Pausa mínima en segundos para separar enunciados.</summary>
    public decimal SeparacionEnunciadosSeg { get; set; } = 0.5m;

    /// <summary>Costo de referencia en USD por minuto de audio.</summary>
    public decimal CostoPorMinutoUsd { get; set; } = 0.0043m;

    public int TiempoEsperaSeg { get; set; } = 120;
}

/// <summary>Sección "Groq". La llave va en el .env (Groq__ApiKey).</summary>
public sealed class OpcionesGroq
{
    public const string Seccion = "Groq";

    public string? ApiKey { get; set; }
    public string UrlBase { get; set; } = "https://api.groq.com/openai/v1/";

    /// <summary>Llama 3.1 70B ya no está habilitado en la cuenta; se usa gpt-oss-120b.</summary>
    public string ModeloEvaluacion { get; set; } = "openai/gpt-oss-120b";

    /// <summary>Modelo para reconocer actores (Agente / Cliente) en cada fragmento.</summary>
    public string ModeloActores { get; set; } = "openai/gpt-oss-120b";

    /// <summary>Precios estimados en USD por millón de tokens.</summary>
    public decimal CostoEntradaPorMillonUsd { get; set; } = 0.15m;

    public decimal CostoSalidaPorMillonUsd { get; set; } = 0.60m;

    public int TiempoEsperaSeg { get; set; } = 90;
}
