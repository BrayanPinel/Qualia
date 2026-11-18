namespace Qualia.Application.Features.Procesamiento;

/// <summary>Configuración del motor (sección "Procesamiento" de appsettings).</summary>
public sealed class OpcionesProcesamiento
{
    public const string Seccion = "Procesamiento";

    /// <summary>Si es false no se registra el worker en segundo plano (pruebas o despliegues sin worker).</summary>
    public bool WorkerHabilitado { get; set; } = true;

    /// <summary>Intentos máximos por trabajo (reintentos limitados, HU-13).</summary>
    public int MaximoIntentos { get; set; } = 3;

    /// <summary>Espera antes del primer reintento; se duplica en cada intento.</summary>
    public TimeSpan EsperaBaseReintento { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>Tiempo máximo que un trabajo puede estar EnProceso antes de considerarse abandonado.</summary>
    public TimeSpan DuracionBloqueo { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Intervalo de sondeo cuando no hay trabajos.</summary>
    public TimeSpan IntervaloSondeo { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Trabajos reclamados por ciclo.</summary>
    public int TamanoLote { get; set; } = 5;

    /// <summary>Si es true, el worker encola solo las llamadas Disponible con rúbrica publicada.</summary>
    public bool EvaluacionAutomatica { get; set; } = true;

    /// <summary>
    /// Puntos alrededor del umbral de cumplimiento en los que la nota de la IA se considera dudosa
    /// y la evaluación pasa a revisión humana. 0 desactiva la regla.
    /// </summary>
    public decimal MargenRevisionUmbral { get; set; } = 5m;

    /// <summary>Pausa antes de volver a encolar llamadas de una empresa sin suscripción activa o sin cuota.</summary>
    public TimeSpan EsperaTrasRechazoConsumo { get; set; } = TimeSpan.FromMinutes(1);
}
