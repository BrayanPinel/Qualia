using Qualia.Domain.Common;
using Qualia.Domain.Enums;

namespace Qualia.Domain.Entidades;

/// Trabajo asíncrono que procesa una llamada (máximo uno por llamada).
/// Pendiente → EnProceso → (ReintentoPendiente → EnProceso)* → Completado | Fallido.
/// Un trabajo Fallido puede reiniciarse (llamada perdida que se vuelve a evaluar) en la misma fila.
/// Mientras está EnProceso, ProximoIntento actúa como fecha de expiración
/// del bloqueo: si el proceso se cae, el trabajo vuelve a poder reclamarse después de esa fecha.
public class TrabajoProcesamiento : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid LlamadaId { get; set; }
    public EstadoTrabajo Estado { get; set; } = EstadoTrabajo.Pendiente;
    public int Intentos { get; set; }
    public DateTime? ProximoIntento { get; set; }

    /// Último error ya saneado (sin datos internos); se expone como motivoFallo.
    public string? UltimoError { get; set; }

    public Llamada? Llamada { get; set; }

    public static TrabajoProcesamiento Crear(Guid empresaId, Guid llamadaId) => new()
    {
        EmpresaId = empresaId,
        LlamadaId = llamadaId,
        Estado = EstadoTrabajo.Pendiente,
        Intentos = 0,
    };

    public bool EstaTerminado => Estado is EstadoTrabajo.Completado or EstadoTrabajo.Fallido;

    public void Completar()
    {
        AsegurarEnProceso();
        Estado = EstadoTrabajo.Completado;
        ProximoIntento = null;
    }

    /// Registra un error recuperable. Si ya se alcanzó el máximo de intentos, el trabajo
    /// falla definitivamente. Devuelve true cuando el fallo es definitivo.
    public bool RegistrarFallo(string errorSaneado, int maximoIntentos, DateTime ahoraUtc, TimeSpan esperaBase)
    {
        AsegurarEnProceso();

        if (Intentos >= maximoIntentos)
        {
            FallarDefinitivamente(errorSaneado);
            return true;
        }

        // Espera exponencial: base, 2×base, 4×base...
        var factor = Math.Pow(2, Math.Max(Intentos - 1, 0));
        Estado = EstadoTrabajo.ReintentoPendiente;
        UltimoError = errorSaneado;
        ProximoIntento = ahoraUtc + TimeSpan.FromTicks((long)(esperaBase.Ticks * factor));
        return false;
    }

    /// Reintento manual de una llamada perdida: vuelve a Pendiente con los intentos en cero.
    public void Reiniciar()
    {
        if (Estado != EstadoTrabajo.Fallido)
        {
            throw new ReglaDominioException(
                "TRANSICION_INVALIDA",
                $"Solo un trabajo Fallido puede reiniciarse (estado actual: {Estado}).");
        }

        Estado = EstadoTrabajo.Pendiente;
        Intentos = 0;
        ProximoIntento = null;
        UltimoError = null;
    }

    public void FallarDefinitivamente(string errorSaneado)
    {
        AsegurarEnProceso();
        Estado = EstadoTrabajo.Fallido;
        UltimoError = errorSaneado;
        ProximoIntento = null;
    }

    private void AsegurarEnProceso()
    {
        if (Estado != EstadoTrabajo.EnProceso)
        {
            throw new ReglaDominioException(
                "TRANSICION_INVALIDA",
                $"El trabajo debe estar EnProceso para cambiar de estado (estado actual: {Estado}).");
        }
    }
}
