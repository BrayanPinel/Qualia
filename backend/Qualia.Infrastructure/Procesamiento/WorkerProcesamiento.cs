using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qualia.Application.Features.Procesamiento;

namespace Qualia.Infrastructure.Procesamiento;

/// <summary>
/// Servicio en segundo plano. En cada ciclo encola las llamadas nuevas (evaluación automática)
/// y procesa la cola de trabajos. Despierta con la señal de IniciarEvaluacion o por sondeo
/// periódico (llamadas nuevas, reintentos y bloqueos vencidos).
/// </summary>
public sealed class WorkerProcesamiento(
    DespachadorEvaluacionAutomatica despachador,
    EjecutorProcesamiento ejecutor,
    ISenalProcesamiento senal,
    IOptions<OpcionesProcesamiento> opciones,
    ILogger<WorkerProcesamiento> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker de procesamiento iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var encoladas = await despachador.EjecutarCicloAsync(stoppingToken);
                var procesados = await ejecutor.EjecutarCicloAsync(stoppingToken);
                if (encoladas > 0 || procesados > 0)
                {
                    continue; // Puede haber más llamadas o trabajos pendientes.
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Base de datos caída u otro error transversal: se reintenta en el siguiente ciclo.
                logger.LogError(ex, "Error en el ciclo del worker de procesamiento");
            }

            try
            {
                await senal.EsperarAsync(opciones.Value.IntervaloSondeo, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

/// <summary>Señal en memoria (un solo proceso) para despertar al worker sin esperar el sondeo.</summary>
public sealed class SenalProcesamiento : ISenalProcesamiento
{
    private readonly SemaphoreSlim _semaforo = new(0, 1);

    public void Notificar()
    {
        try
        {
            _semaforo.Release();
        }
        catch (SemaphoreFullException)
        {
            // Ya hay una notificación pendiente.
        }
    }

    public Task EsperarAsync(TimeSpan maximo, CancellationToken cancellationToken) =>
        _semaforo.WaitAsync(maximo, cancellationToken);
}
