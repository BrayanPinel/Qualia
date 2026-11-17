namespace Qualia.Application.Common.Interfaces;

/// <summary>
/// CONTRATO D ↔ C (reserva y cierre de consumo).
/// Lo expone el Rol C y lo consume el Rol D. Mientras C no lo implemente, se usa
/// ConsumoEvaluacionProvisional (Infrastructure), que NO persiste nada.
///
/// Reglas para la implementación:
/// Los tres métodos se invocan DENTRO de una transacción abierta por el llamador sobre el mismo
/// IQualiaDbContext del scope. La implementación puede usar SaveChanges o SQL con bloqueo
/// (SELECT ... FOR UPDATE), pero NO debe hacer Commit ni abrir otra transacción.
/// Son idempotentes por (empresaId, llamadaId): un reintento no duplica reserva ni facturación.
/// Los errores de negocio se lanzan como ExcepcionNegocio.
/// </summary>
public interface IConsumoEvaluacion
{
    /// <summary>
    /// Verifica que la suscripción vigente permita evaluar y reserva
    /// ceil(duracionSeg / 60, 2 decimales) minutos en el periodo actual (Consumo_llamada = Reservado).
    /// </summary>

    /// <exception cref="Errores.ExcepcionNegocio">
    /// 403 SUSCRIPCION_NO_ACTIVA (EnMora / Vencida) · 409 CUOTA_AGOTADA.
    /// </exception>
    Task ReservarAsync(Guid empresaId, Guid llamadaId, decimal duracionSeg, CancellationToken cancellationToken);

    /// <summary>Evaluación completada: Reservado → Facturado y acumula el uso técnico.</summary>
    Task FacturarAsync(Guid empresaId, Guid llamadaId, UsoTecnicoIA uso, CancellationToken cancellationToken);

    /// <summary>Fallo definitivo: Reservado → Liberado (minutos en cero) y acumula el uso técnico.</summary>
    Task LiberarAsync(Guid empresaId, Guid llamadaId, UsoTecnicoIA uso, CancellationToken cancellationToken);
}

/// <summary>
/// Uso técnico de los proveedores de IA en UN intento de procesamiento.
/// C lo suma a Consumo_llamada (tokens y costo se acumulan incluso si la llamada falla).
/// </summary>

public sealed record UsoTecnicoIA(
    int TokensTranscriptor,
    int TokensEntradaEvaluador,
    int TokensSalidaEvaluador,
    decimal CostoEstimado)
{
    public static readonly UsoTecnicoIA Cero = new(0, 0, 0, 0m);

    public UsoTecnicoIA Sumar(UsoTecnicoIA otro) => new(
        TokensTranscriptor + otro.TokensTranscriptor,
        TokensEntradaEvaluador + otro.TokensEntradaEvaluador,
        TokensSalidaEvaluador + otro.TokensSalidaEvaluador,
        CostoEstimado + otro.CostoEstimado);
}
