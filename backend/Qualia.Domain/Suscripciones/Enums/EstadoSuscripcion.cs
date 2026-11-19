namespace Qualia.Domain.Suscripciones;

/// Estados de Suscripcion. Se persisten como texto.
public enum EstadoSuscripcion
{
    /// Permite operar dentro de la cuota de prueba.
    Prueba,

    /// Permite operar según el plan.
    Activa,

    /// Permite consulta; bloquea nuevas evaluaciones.
    EnMora,

    /// Permite consulta durante la gracia; bloquea el inicio de evaluaciones.
    Vencida,
}
