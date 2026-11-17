using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;

namespace Qualia.Application.Common.Interfaces;

/// <summary>
/// Contrato compartido (expuesto por Rol C, consumido por Rol D al iniciar evaluación
/// y durante el procesamiento asíncrono). Garantiza reservas atómicas e idempotentes (RN-13).
/// </summary>
public interface IConsumoEvaluacion
{
    /// <summary>
    /// Comprueba suscripción operativa y saldo, y reserva minutos de forma atómica.
    /// Si la llamada ya tiene reserva, devuelve el registro existente sin duplicar consumo.
    /// </summary>
    Task<Resultado<ConsumoLlamadaDto>> ReservarAsync(
        ReservarConsumoRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acumula tokens y costo técnico por intento de procesamiento, incluso si falla.
    /// </summary>
    Task<Resultado> RegistrarUsoTecnicoAsync(
        RegistrarUsoTecnicoRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Confirma la reserva como facturable al completar el procesamiento (idempotente).</summary>
    Task<Resultado> FacturarAsync(
        Guid empresaId,
        Guid llamadaId,
        CancellationToken cancellationToken = default);

    /// <summary>Libera los minutos reservados ante fallo definitivo del trabajo (idempotente).</summary>
    Task<Resultado> LiberarAsync(
        Guid empresaId,
        Guid llamadaId,
        CancellationToken cancellationToken = default);
}

public sealed record ReservarConsumoRequest(
    Guid EmpresaId,
    Guid LlamadaId,
    decimal DuracionSeg,
    DateTime? FechaInicioUtc = null);

public sealed record RegistrarUsoTecnicoRequest(
    Guid EmpresaId,
    Guid LlamadaId,
    int TokensTranscriptor,
    int TokensEntradaEvaluador,
    int TokensSalidaEvaluador,
    decimal CostoEstimado);
