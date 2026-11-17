using Qualia.Domain.Common;

namespace Qualia.Domain.Suscripciones;

/// <summary>
/// Periodo de consumo mensual (tabla empresarial <c>periodo_consumo</c>).
/// Existe a lo sumo un periodo por empresa y mes: todos terminan el primer día
/// del mes siguiente, por eso se garantiza con UNIQUE <c>(empresa_id, fecha_fin)</c>.
/// <c>minutos_contratados</c> se fija al abrirlo y no se recalcula (fase1 §7.8).
/// </summary>
public sealed class PeriodoConsumo : IEntidadEmpresa
{
    // Constructor para EF Core.
    private PeriodoConsumo()
    {
    }

    public Guid EmpresaId { get; private set; }

    public Guid Id { get; private set; }

    public Guid SuscripcionId { get; private set; }

    public DateTime FechaInicio { get; private set; }

    public DateTime FechaFin { get; private set; }

    public decimal MinutosContratados { get; private set; }

    public Suscripcion? Suscripcion { get; private set; }

    /// <summary>Mes calendario UTC al que pertenece el periodo.</summary>
    public PeriodoMensual Mes => PeriodoMensual.DesdeFecha(FechaInicio);

    /// <summary>
    /// Abre el periodo del <paramref name="mes"/> para la suscripción. Nunca comienza
    /// antes del inicio de la suscripción: el primer periodo arranca al registrar la
    /// empresa y no se asigna consumo anterior al registro.
    /// </summary>
    public static PeriodoConsumo Abrir(Suscripcion suscripcion, PeriodoMensual mes, decimal minutosContratados)
    {
        ArgumentNullException.ThrowIfNull(suscripcion);
        ArgumentOutOfRangeException.ThrowIfNegative(minutosContratados);

        var inicio = suscripcion.FechaInicio > mes.InicioUtc ? suscripcion.FechaInicio : mes.InicioUtc;
        if (inicio >= mes.FinUtc)
        {
            throw new InvalidOperationException(
                $"La suscripción inicia después del periodo {mes}; no se puede abrir ese periodo.");
        }

        return new PeriodoConsumo
        {
            EmpresaId = suscripcion.EmpresaId,
            Id = Guid.CreateVersion7(),
            // Solo FK compuesta (empresa_id, suscripcion_id); EF ordena los INSERT
            // por la relación del modelo aunque ambos se agreguen en el mismo SaveChanges.
            SuscripcionId = suscripcion.Id,
            FechaInicio = inicio,
            FechaFin = mes.FinUtc,
            MinutosContratados = minutosContratados,
        };
    }

    /// <summary>
    /// Sustituye los datos de plan del periodo del mes cuando aún no registra ningún
    /// consumo de llamada, tras un cambio de plan aprobado (fase1 §7.8).
    /// </summary>
    public void ReemplazarPlanEnPeriodoVacio(Suscripcion nuevaSuscripcion, decimal minutosContratados)
    {
        ArgumentNullException.ThrowIfNull(nuevaSuscripcion);
        if (nuevaSuscripcion.EmpresaId != EmpresaId)
        {
            throw new InvalidOperationException("La suscripción pertenece a otra empresa.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(minutosContratados);

        SuscripcionId = nuevaSuscripcion.Id;
        MinutosContratados = minutosContratados;
    }
}
