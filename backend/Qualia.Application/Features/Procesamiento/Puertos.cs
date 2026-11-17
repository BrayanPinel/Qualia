using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Procesamiento;

// Puertos del motor de evaluación. Las implementaciones viven en Infrastructure:
// hoy son simuladas y reproducibles (modo Simulado); un adaptador real
// solo debe implementar estas interfaces.

/// <summary>Convierte el audio de una llamada en segmentos con hablante y tiempos.</summary>
public interface ITranscriptor
{
    Task<ResultadoTranscripcion> TranscribirAsync(SolicitudTranscripcion solicitud, CancellationToken cancellationToken);
}

/// <summary>Evalúa la transcripción (ya enmascarada) contra los criterios de la rúbrica fijada en la llamada.</summary>
public interface IEvaluadorIA
{
    Task<ResultadoEvaluacionIA> EvaluarAsync(SolicitudEvaluacion solicitud, CancellationToken cancellationToken);
}

/// <summary>Reemplaza datos sensibles (correos, teléfonos, tarjetas, documentos) antes de almacenar o evaluar (RN-14).</summary>
public interface IEnmascaradorDatos
{
    string Enmascarar(string texto);
}

/// <summary>Despierta al worker cuando hay trabajo nuevo (evita esperar al siguiente sondeo).</summary>
public interface ISenalProcesamiento
{
    void Notificar();
    Task EsperarAsync(TimeSpan maximo, CancellationToken cancellationToken);
}

// ---------------------------- Modelos de los puertos ----------------------------

public sealed record SolicitudTranscripcion(Guid EmpresaId, Guid LlamadaId, string ArchivoRuta, decimal DuracionSeg);

public sealed record SegmentoTranscrito(string Hablante, decimal InicioSeg, decimal FinSeg, string Texto);

public sealed record ResultadoTranscripcion(
    IReadOnlyList<SegmentoTranscrito> Segmentos,
    int TokensConsumidos,
    decimal CostoEstimado);

public sealed record CriterioParaEvaluar(
    Guid CriterioId,
    string Nombre,
    string Descripcion,
    decimal Peso,
    bool EsCritico,
    decimal UmbralCumplimiento);

/// <summary>Segmento enviado al evaluador: el texto ya está enmascarado.</summary>
public sealed record SegmentoParaEvaluar(Guid SegmentoId, int Orden, string Hablante, string Texto);

public sealed record SolicitudEvaluacion(
    Guid LlamadaId,
    IReadOnlyList<CriterioParaEvaluar> Criterios,
    IReadOnlyList<SegmentoParaEvaluar> Segmentos);

/// <summary>Evidencia sugerida por el modelo; se valida antes de guardarse (RN-09).</summary>
public sealed record EvidenciaPropuesta(Guid SegmentoId, string CitaTextual);

public sealed record CriterioEvaluado(
    Guid CriterioId,
    decimal Puntaje,
    string Justificacion,
    IReadOnlyList<EvidenciaPropuesta> Evidencias);

public sealed record ResultadoEvaluacionIA(
    string ModeloUsado,
    ModoIA Modo,
    IReadOnlyList<CriterioEvaluado> Criterios,
    int TokensEntrada,
    int TokensSalida,
    decimal CostoEstimado);
