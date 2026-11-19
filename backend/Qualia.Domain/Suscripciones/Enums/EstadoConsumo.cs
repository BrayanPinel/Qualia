namespace Qualia.Domain.Suscripciones;

/// Estados de ConsumoLlamada. Se persisten como texto.
public enum EstadoConsumo
{
    /// Minutos reservados al encolar la llamada.
    Reservado,

    /// Procesamiento completado: la reserva pasa a facturable.
    Facturado,

    /// Fallo definitivo: reservados y facturables quedan en cero.
    Liberado,
}
