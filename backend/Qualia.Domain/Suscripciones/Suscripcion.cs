using Qualia.Domain.Common;

namespace Qualia.Domain.Suscripciones;

/// <summary>
/// Suscripción de una empresa (tabla empresarial <c>suscripcion</c>,
/// PK compuesta <c>(empresa_id, id)</c>). Solo puede existir una vigente
/// (Prueba, Activa o EnMora) por empresa: índice único parcial.
/// </summary>
public sealed class Suscripcion : IEntidadEmpresa
{
    public const int LongitudMaximaEstado = 50;

    // Constructor para EF Core.
    private Suscripcion()
    {
    }

    public Guid EmpresaId { get; private set; }

    public Guid Id { get; private set; }

    public Guid PlanId { get; private set; }

    public DateTime FechaInicio { get; private set; }

    public DateTime FechaVencimiento { get; private set; }

    public EstadoSuscripcion Estado { get; private set; }

    public bool PermiteExcedente { get; private set; }

    public DateTime? GraciaHasta { get; private set; }

    public Plan? Plan { get; private set; }

    /// <summary>Prueba, Activa o EnMora (fase1 §6.5 "Unicidad").</summary>
    public bool EsVigente => EsEstadoVigente(Estado);

    public static bool EsEstadoVigente(EstadoSuscripcion estado) =>
        estado is EstadoSuscripcion.Prueba or EstadoSuscripcion.Activa or EstadoSuscripcion.EnMora;

    /// <summary>
    /// Determina si la suscripción permite iniciar nuevas evaluaciones en el instante UTC indicado
    /// (fase1 §2.4): solo <see cref="EstadoSuscripcion.Prueba"/> o <see cref="EstadoSuscripcion.Activa"/>
    /// mientras no haya vencido.
    /// </summary>
    public bool PermiteNuevasEvaluaciones(DateTime ahoraUtc)
    {
        FechasUtc.Exigir(ahoraUtc, nameof(ahoraUtc));
        return Estado is EstadoSuscripcion.Prueba or EstadoSuscripcion.Activa &&
               ahoraUtc < FechaVencimiento;
    }

    /// <summary>
    /// Suscripción de prueba creada al registrar la empresa (HU-01, fase1 §7.8):
    /// dura hasta el inicio del mes siguiente al registro, sin prorrateo
    /// y sin permiso de excedente.
    /// </summary>
    public static Suscripcion CrearPrueba(Guid empresaId, Plan plan, DateTime fechaRegistroUtc)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("La empresa es obligatoria.", nameof(empresaId));
        }

        if (!plan.Activo)
        {
            throw new InvalidOperationException("No se puede suscribir una empresa a un plan inactivo.");
        }

        FechasUtc.Exigir(fechaRegistroUtc, nameof(fechaRegistroUtc));

        return new Suscripcion
        {
            EmpresaId = empresaId,
            Id = Guid.CreateVersion7(),
            // Solo FK: no se asigna la navegación para que EF nunca intente
            // insertar un Plan cargado sin seguimiento.
            PlanId = plan.Id,
            FechaInicio = fechaRegistroUtc,
            FechaVencimiento = PeriodoMensual.DesdeFecha(fechaRegistroUtc).FinUtc,
            Estado = EstadoSuscripcion.Prueba,
            PermiteExcedente = false,
            GraciaHasta = null,
        };
    }

    /// <summary>
    /// Pago simulado aprobado (fase1 §7.8): activa o renueva la suscripción hasta
    /// el inicio del mes siguiente al pago. Nunca acorta una vigencia mayor ya
    /// concedida. La cuota del mes la gestiona <see cref="PeriodoConsumo"/>.
    /// </summary>
    public void RegistrarPagoAprobado(DateTime fechaPagoUtc)
    {
        var finMes = PeriodoMensual.DesdeFecha(fechaPagoUtc).FinUtc;

        Estado = EstadoSuscripcion.Activa;
        if (finMes > FechaVencimiento)
        {
            FechaVencimiento = finMes;
        }

        GraciaHasta = null;
    }

    /// <summary>
    /// Pago simulado rechazado: no renueva ni concede minutos. Si la vigencia ya
    /// terminó con la renovación pendiente, la suscripción pasa a EnMora.
    /// Devuelve <c>true</c> si el estado cambió.
    /// </summary>
    public bool RegistrarPagoRechazado(DateTime fechaUtc)
    {
        FechasUtc.Exigir(fechaUtc, nameof(fechaUtc));

        if (Estado is EstadoSuscripcion.Prueba or EstadoSuscripcion.Activa &&
            fechaUtc >= FechaVencimiento)
        {
            Estado = EstadoSuscripcion.EnMora;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Crea la nueva suscripción al cambiar de plan desde administración comercial (fase1 §7.8).
    /// </summary>
    public static Suscripcion CrearPorCambioDePlan(
        Guid empresaId,
        Plan plan,
        EstadoSuscripcion estado,
        DateTime fechaInicioUtc,
        DateTime fechaVencimientoUtc,
        bool permiteExcedente,
        DateTime? graciaHastaUtc = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("La empresa es obligatoria.", nameof(empresaId));
        }

        if (!plan.Activo)
        {
            throw new InvalidOperationException("No se puede asignar un plan inactivo.");
        }

        if (!Enum.IsDefined(estado))
        {
            throw new ArgumentOutOfRangeException(nameof(estado));
        }

        FechasUtc.Exigir(fechaInicioUtc, nameof(fechaInicioUtc));
        FechasUtc.Exigir(fechaVencimientoUtc, nameof(fechaVencimientoUtc));

        if (fechaVencimientoUtc <= fechaInicioUtc)
        {
            throw new ArgumentException(
                "La fecha de vencimiento debe ser posterior a la fecha de inicio.",
                nameof(fechaVencimientoUtc));
        }

        if (graciaHastaUtc.HasValue)
        {
            FechasUtc.Exigir(graciaHastaUtc.Value, nameof(graciaHastaUtc));
            if (graciaHastaUtc.Value < fechaVencimientoUtc)
            {
                throw new ArgumentException(
                    "La fecha de gracia no puede preceder al vencimiento.",
                    nameof(graciaHastaUtc));
            }
        }

        return new Suscripcion
        {
            EmpresaId = empresaId,
            Id = Guid.CreateVersion7(),
            PlanId = plan.Id,
            FechaInicio = fechaInicioUtc,
            FechaVencimiento = fechaVencimientoUtc,
            Estado = estado,
            PermiteExcedente = permiteExcedente,
            GraciaHasta = graciaHastaUtc,
        };
    }

    /// <summary>
    /// Cierra la suscripción anterior al realizar un cambio de plan para mantener
    /// una única suscripción vigente por empresa (fase1 §6.5 y §7.8).
    /// </summary>
    public void CerrarPorCambioDePlan()
    {
        Estado = EstadoSuscripcion.Vencida;
        GraciaHasta = null;
    }

    /// <summary>
    /// Actualiza estado, vencimiento y permiso de excedente sin cambiar de plan ni
    /// conceder cuota adicional (fase1 §7.8).
    /// </summary>
    public void ActualizarCondicionesComerciales(
        EstadoSuscripcion estado,
        DateTime fechaVencimientoUtc,
        bool permiteExcedente,
        DateTime? graciaHastaUtc = null)
    {
        if (!Enum.IsDefined(estado))
        {
            throw new ArgumentOutOfRangeException(nameof(estado));
        }

        FechasUtc.Exigir(fechaVencimientoUtc, nameof(fechaVencimientoUtc));
        if (fechaVencimientoUtc <= FechaInicio)
        {
            throw new ArgumentException(
                "La fecha de vencimiento debe ser posterior a la fecha de inicio de la suscripción.",
                nameof(fechaVencimientoUtc));
        }

        if (graciaHastaUtc.HasValue)
        {
            FechasUtc.Exigir(graciaHastaUtc.Value, nameof(graciaHastaUtc));
            if (graciaHastaUtc.Value < fechaVencimientoUtc)
            {
                throw new ArgumentException(
                    "La fecha de gracia no puede preceder al vencimiento.",
                    nameof(graciaHastaUtc));
            }
        }

        Estado = estado;
        FechaVencimiento = fechaVencimientoUtc;
        PermiteExcedente = permiteExcedente;
        GraciaHasta = graciaHastaUtc;
    }
}
