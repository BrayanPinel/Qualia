using Qualia.Domain.Common;

namespace Qualia.Domain.Suscripciones;

/// <summary>
/// Consumo asociado a una llamada (tabla empresarial <c>consumo_llamada</c>).
/// Cero o uno por llamada: UNIQUE <c>(empresa_id, llamada_id)</c>.
/// Ciclo de vida (fase1 §6.5): nace <see cref="EstadoConsumo.Reservado"/>; al completar
/// pasa a <see cref="EstadoConsumo.Facturado"/> y ante fallo definitivo a
/// <see cref="EstadoConsumo.Liberado"/>. Los reintentos reutilizan el mismo registro.
/// </summary>
public sealed class ConsumoLlamada : IEntidadEmpresa
{
    public const int LongitudMaximaEstado = 20;

    // Constructor para EF Core.
    private ConsumoLlamada()
    {
    }

    public Guid EmpresaId { get; private set; }

    public Guid Id { get; private set; }

    public Guid LlamadaId { get; private set; }

    public Guid PeriodoId { get; private set; }

    public decimal MinutosFacturables { get; private set; }

    public int TokensTranscriptor { get; private set; }

    public int TokensEntradaEvaluador { get; private set; }

    public int TokensSalidaEvaluador { get; private set; }

    public decimal CostoEstimado { get; private set; }

    public decimal MinutosReservados { get; private set; }

    public EstadoConsumo Estado { get; private set; }

    public PeriodoConsumo? Periodo { get; private set; }

    /// <summary>
    /// <c>duracion_seg / 60</c> redondeado hacia arriba a dos decimales (fase1 §7.8).
    /// </summary>
    public static decimal CalcularMinutos(decimal duracionSeg)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(duracionSeg);
        return Math.Ceiling(duracionSeg / 60m * 100m) / 100m;
    }

    /// <summary>Crea la reserva inicial de minutos contra el periodo indicado.</summary>
    public static ConsumoLlamada Reservar(PeriodoConsumo periodo, Guid llamadaId, decimal duracionSeg)
    {
        ArgumentNullException.ThrowIfNull(periodo);
        if (llamadaId == Guid.Empty)
        {
            throw new ArgumentException("La llamada es obligatoria.", nameof(llamadaId));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duracionSeg);

        return new ConsumoLlamada
        {
            EmpresaId = periodo.EmpresaId,
            Id = Guid.CreateVersion7(),
            LlamadaId = llamadaId,
            PeriodoId = periodo.Id,
            MinutosReservados = CalcularMinutos(duracionSeg),
            MinutosFacturables = 0m,
            Estado = EstadoConsumo.Reservado,
        };
    }

    /// <summary>Acumula el costo técnico, incluso si el procesamiento falla.</summary>
    public void AcumularCostoTecnico(
        int tokensTranscriptor,
        int tokensEntradaEvaluador,
        int tokensSalidaEvaluador,
        decimal costoEstimado)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tokensTranscriptor);
        ArgumentOutOfRangeException.ThrowIfNegative(tokensEntradaEvaluador);
        ArgumentOutOfRangeException.ThrowIfNegative(tokensSalidaEvaluador);
        ArgumentOutOfRangeException.ThrowIfNegative(costoEstimado);

        TokensTranscriptor += tokensTranscriptor;
        TokensEntradaEvaluador += tokensEntradaEvaluador;
        TokensSalidaEvaluador += tokensSalidaEvaluador;
        CostoEstimado += costoEstimado;
    }

    /// <summary>Mueve la reserva a facturable. Idempotente si ya estaba facturado.</summary>
    public void Facturar()
    {
        switch (Estado)
        {
            case EstadoConsumo.Facturado:
                return;
            case EstadoConsumo.Liberado:
                throw new InvalidOperationException("Un consumo liberado no puede facturarse.");
            default:
                MinutosFacturables = MinutosReservados;
                MinutosReservados = 0m;
                Estado = EstadoConsumo.Facturado;
                return;
        }
    }

    /// <summary>Fallo definitivo: deja ambos importes en cero. Idempotente.</summary>
    public void Liberar()
    {
        switch (Estado)
        {
            case EstadoConsumo.Liberado:
                return;
            case EstadoConsumo.Facturado:
                throw new InvalidOperationException("Un consumo facturado no puede liberarse.");
            default:
                MinutosReservados = 0m;
                MinutosFacturables = 0m;
                Estado = EstadoConsumo.Liberado;
                return;
        }
    }
}
