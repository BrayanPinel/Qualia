using System.Collections.Concurrent;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Procesamiento;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Infrastructure.Procesamiento;

namespace Qualia.Tests.Integracion.Infraestructura;

/// Doble del contrato del Rol C: registra las llamadas para verificar el protocolo D ↔ C.
public sealed class ConsumoEvaluacionRegistrado : IConsumoEvaluacion
{
    public ConcurrentQueue<(string Operacion, Guid LlamadaId)> Operaciones { get; } = new();

    /// Si se define, ReservarAsync devuelve este error (p. ej. SUSCRIPCION_NO_ACTIVA).
    public Func<Guid, ErrorAplicacion?> RechazarReserva { get; set; } = _ => null;

    public int Contar(string operacion, Guid llamadaId) =>
        Operaciones.Count(o => o.Operacion == operacion && o.LlamadaId == llamadaId);

    public Task<Resultado<ConsumoLlamadaDto>> ReservarAsync(ReservarConsumoRequest request, CancellationToken cancellationToken = default)
    {
        if (RechazarReserva(request.LlamadaId) is { } error)
        {
            return Task.FromResult<Resultado<ConsumoLlamadaDto>>(error);
        }

        Operaciones.Enqueue(("Reservar", request.LlamadaId));
        return Task.FromResult<Resultado<ConsumoLlamadaDto>>(new ConsumoLlamadaDto(
            Guid.NewGuid(), request.EmpresaId, request.LlamadaId, Guid.NewGuid(),
            Math.Ceiling(request.DuracionSeg / 60m * 100m) / 100m, 0m, "Reservado"));
    }

    public Task<Resultado> RegistrarUsoTecnicoAsync(RegistrarUsoTecnicoRequest request, CancellationToken cancellationToken = default)
    {
        Operaciones.Enqueue(("UsoTecnico", request.LlamadaId));
        return Task.FromResult(Resultado.Exito());
    }

    public Task<Resultado> FacturarAsync(Guid empresaId, Guid llamadaId, CancellationToken cancellationToken = default)
    {
        Operaciones.Enqueue(("Facturar", llamadaId));
        return Task.FromResult(Resultado.Exito());
    }

    public Task<Resultado> LiberarAsync(Guid empresaId, Guid llamadaId, CancellationToken cancellationToken = default)
    {
        Operaciones.Enqueue(("Liberar", llamadaId));
        return Task.FromResult(Resultado.Exito());
    }
}

/// Transcriptor que falla un número configurable de veces antes de delegar en el simulado.
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
