using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;

namespace Qualia.Application.Common.Interfaces;

/// Contrato compartido (expuesto por Rol C, consumido por Rol D al iniciar evaluación
/// y durante el procesamiento asíncrono). Garantiza reservas atómicas e idempotentes (RN-13).
public interface IConsumoEvaluacion
{
    /// Comprueba suscripción operativa y saldo, y reserva minutos de forma atómica.
    /// Si la llamada ya tiene reserva, devuelve el registro existente sin duplicar consumo.
    Task<Resultado<ConsumoLlamadaDto>> ReservarAsync(
        ReservarConsumoRequest request,
        CancellationToken cancellationToken = default);

    /// Acumula tokens y costo técnico por intento de procesamiento, incluso si falla.
    Task<Resultado> RegistrarUsoTecnicoAsync(
        RegistrarUsoTecnicoRequest request,
        CancellationToken cancellationToken = default);

    /// Confirma la reserva como facturable al completar el procesamiento (idempotente).
    Task<Resultado> FacturarAsync(
        Guid empresaId,
        Guid llamadaId,
        CancellationToken cancellationToken = default);

    /// Libera los minutos reservados ante fallo definitivo del trabajo (idempotente).
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
