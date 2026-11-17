namespace Qualia.Domain.Suscripciones;

/// <summary>
/// Estados de <see cref="ConsumoLlamada"/>. Se persisten como texto.
/// </summary>
public enum EstadoConsumo
{
    /// <summary>Minutos reservados al encolar la llamada.</summary>
    Reservado,

    /// <summary>Procesamiento completado: la reserva pasa a facturable.</summary>
    Facturado,

    /// <summary>Fallo definitivo: reservados y facturables quedan en cero.</summary>
    Liberado,
}
