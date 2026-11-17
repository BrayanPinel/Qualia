using Qualia.Domain.Comun;
using Qualia.Domain.Enums;

namespace Qualia.Domain.Entidades;

/// <summary>
/// Llamada ficticia precargada. Conserva la versión de rúbrica fijada al ingresar (RN-07):
/// la campaña y la versión se obtienen a través de <see cref="Rubrica"/>.
/// </summary>
public class Llamada : IEntidadEmpresa
{
    // Transiciones válidas del ciclo de procesamiento.
    // Volver a EnCola se usa para reintentos y para recuperar un trabajo interrumpido.
    private static readonly Dictionary<EstadoLlamada, EstadoLlamada[]> Transiciones = new()
    {
        [EstadoLlamada.Disponible] = [EstadoLlamada.EnCola],
        [EstadoLlamada.EnCola] = [EstadoLlamada.Transcribiendo, EstadoLlamada.Transcrita, EstadoLlamada.Fallida],
        [EstadoLlamada.Transcribiendo] = [EstadoLlamada.Transcrita, EstadoLlamada.EnCola, EstadoLlamada.Fallida],
        [EstadoLlamada.Transcrita] = [EstadoLlamada.Evaluando, EstadoLlamada.EnCola, EstadoLlamada.Fallida],
        [EstadoLlamada.Evaluando] = [EstadoLlamada.EvaluadaPorIA, EstadoLlamada.EnCola, EstadoLlamada.Fallida],
        [EstadoLlamada.EvaluadaPorIA] = [],
        [EstadoLlamada.Fallida] = [],
    };

    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid RubricaId { get; set; }
    public Guid AgenteId { get; set; }

    /// <summary>Referencia interna al audio. Nunca se expone en las respuestas de la API.</summary>
    public string ArchivoRuta { get; set; } = string.Empty;

    public decimal DuracionSeg { get; set; }
    public DateTime FechaLlamada { get; set; }
    public EstadoLlamada Estado { get; set; } = EstadoLlamada.Disponible;
    public DateTime FechaRegistro { get; set; }

    public Rubrica? Rubrica { get; set; }
    public Usuario? Agente { get; set; }
    public TrabajoProcesamiento? Trabajo { get; set; }
    public EvaluacionIA? Evaluacion { get; set; }
    public ICollection<SegmentoTranscripcion> Segmentos { get; set; } = [];

    public void CambiarEstado(EstadoLlamada nuevo)
    {
        if (nuevo == Estado)
        {
            return;
        }

        if (!Transiciones[Estado].Contains(nuevo))
        {
            throw new ReglaDominioException(
                "TRANSICION_INVALIDA",
                $"La llamada no puede pasar de {Estado} a {nuevo}.");
        }

        Estado = nuevo;
    }

    /// <summary>
    /// Al iniciar un intento de procesamiento, una llamada que quedó en un estado
    /// intermedio (por ejemplo, tras una caída del proceso) vuelve a EnCola.
    /// </summary>
    public void ReanudarEnCola()
    {
        if (Estado is EstadoLlamada.Transcribiendo or EstadoLlamada.Transcrita or EstadoLlamada.Evaluando)
        {
            Estado = EstadoLlamada.EnCola;
        }
    }
}
