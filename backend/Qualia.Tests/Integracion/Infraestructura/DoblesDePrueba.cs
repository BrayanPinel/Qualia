using System.Collections.Concurrent;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Errores;
using Qualia.Application.Features.Procesamiento;
using Qualia.Infrastructure.Procesamiento;

namespace Qualia.Tests.Integracion.Infraestructura;

/// <summary>Doble del contrato del Rol C: registra las llamadas para verificar el protocolo D ↔ C.</summary>
public sealed class ConsumoEvaluacionRegistrado : IConsumoEvaluacion
{
    public ConcurrentQueue<(string Operacion, Guid LlamadaId)> Operaciones { get; } = new();

    /// <summary>Si se define, ReservarAsync lanza esta excepción (p. ej. SUSCRIPCION_NO_ACTIVA).</summary>
    public Func<Guid, ExcepcionNegocio?> RechazarReserva { get; set; } = _ => null;

    public int Contar(string operacion, Guid llamadaId) =>
        Operaciones.Count(o => o.Operacion == operacion && o.LlamadaId == llamadaId);

    public Task ReservarAsync(Guid empresaId, Guid llamadaId, decimal duracionSeg, CancellationToken cancellationToken)
    {
        if (RechazarReserva(llamadaId) is { } error)
        {
            throw error;
        }

        Operaciones.Enqueue(("Reservar", llamadaId));
        return Task.CompletedTask;
    }

    public Task FacturarAsync(Guid empresaId, Guid llamadaId, UsoTecnicoIA uso, CancellationToken cancellationToken)
    {
        Operaciones.Enqueue(("Facturar", llamadaId));
        return Task.CompletedTask;
    }

    public Task LiberarAsync(Guid empresaId, Guid llamadaId, UsoTecnicoIA uso, CancellationToken cancellationToken)
    {
        Operaciones.Enqueue(("Liberar", llamadaId));
        return Task.CompletedTask;
    }
}

/// <summary>Transcriptor que falla un número configurable de veces antes de delegar en el simulado.</summary>
public sealed class TranscriptorConFallos(int fallosAntesDeFuncionar) : ITranscriptor
{
    private readonly TranscriptorSimulado _real = new();
    private readonly ConcurrentDictionary<Guid, int> _intentos = new();

    public Task<ResultadoTranscripcion> TranscribirAsync(SolicitudTranscripcion solicitud, CancellationToken cancellationToken)
    {
        var intento = _intentos.AddOrUpdate(solicitud.LlamadaId, 1, (_, n) => n + 1);
        if (intento <= fallosAntesDeFuncionar)
        {
            // Mensaje interno que NO debe llegar al cliente.
            throw new InvalidOperationException("Host=db-interna;Password=secreta — fallo simulado del proveedor");
        }

        return _real.TranscribirAsync(solicitud, cancellationToken);
    }
}
