namespace Qualia.Domain.Suscripciones;

/// <summary>
/// Estados de <see cref="Suscripcion"/> (fase1 §2.4 y §7.7). Se persisten como texto.
/// </summary>
public enum EstadoSuscripcion
{
    /// <summary>Permite operar dentro de la cuota de prueba.</summary>
    Prueba,

    /// <summary>Permite operar según el plan.</summary>
    Activa,

    /// <summary>Permite consulta; bloquea nuevas evaluaciones.</summary>
    EnMora,

    /// <summary>Permite consulta durante la gracia; bloquea el inicio de evaluaciones.</summary>
    Vencida,
}
