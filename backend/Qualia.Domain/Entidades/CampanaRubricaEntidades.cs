using Qualia.Domain.Common;
using Qualia.Domain.Enums;

namespace Qualia.Domain.Entidades;

// Rol B — Campañas, asignaciones y rúbricas versionadas.

/// <summary>Tabla campana.</summary>
public class Campana : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

/// <summary>
/// Tabla puente usuario_campana. PK (empresa_id, usuario_id, campana_id).
/// Solo se asignan usuarios con rol Agente o Supervisor de la misma empresa (RN-03).
/// </summary>
public class UsuarioCampana : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid CampanaId { get; set; }

    public static bool RolAsignable(RolEmpresa rol) => rol is RolEmpresa.Agente or RolEmpresa.Supervisor;
}

/// <summary>
/// Tabla rubrica: una VERSIÓN de rúbrica de una campaña.
/// Borrador → Activa (publicada) → Retirada. Una versión publicada es inmutable (RN-04)
/// y una campaña tiene como máximo una versión Activa (índice único parcial).
/// </summary>
public class Rubrica : IEntidadEmpresa
{
    public const int MinimoCriterios = 1;
    public const int MaximoCriterios = 30;
    public const decimal SumaPesosRequerida = 100m;

    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CampanaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public EstadoRubrica Estado { get; set; } = EstadoRubrica.Borrador;
    public DateTime FechaCreacion { get; set; }

    public ICollection<CriterioRubrica> Criterios { get; set; } = [];

    /// <summary>Una rúbrica Activa o Retirada ya fue publicada y es inmutable (RN-04).</summary>
    public bool EstaPublicada => Estado != EstadoRubrica.Borrador;

    /// <summary>Solo un borrador puede modificarse; una versión publicada nunca cambia (RN-04).</summary>
    public void AsegurarEditable()
    {
        if (EstaPublicada)
        {
            throw new ReglaDominioException(
                "RUBRICA_PUBLICADA",
                "Una rúbrica publicada no se puede modificar; cree una nueva versión.");
        }
    }

    /// <summary>
    /// Publica el borrador: exige entre 1 y 30 criterios, pesos positivos que sumen exactamente 100,
    /// umbrales entre 0 y 100 y nombres de criterio sin duplicar (RN-04, RN-05).
    /// </summary>
    public void Publicar()
    {
        AsegurarEditable();

        var errores = ErroresDePublicacion(Criterios);
        if (errores.Count > 0)
        {
            throw new ReglaDominioException("RUBRICA_INVALIDA", string.Join(" ", errores));
        }

        Estado = EstadoRubrica.Activa;
    }

    /// <summary>La versión activa deja de usarse para nuevas llamadas; se conserva para las históricas (RN-07).</summary>
    public void Retirar()
    {
        if (Estado != EstadoRubrica.Activa)
        {
            throw new ReglaDominioException("TRANSICION_INVALIDA", $"Solo una rúbrica Activa puede retirarse (estado actual: {Estado}).");
        }

        Estado = EstadoRubrica.Retirada;
    }

    /// <summary>Reglas que debe cumplir el conjunto de criterios para publicarse. Lista vacía = válido.</summary>
    public static IReadOnlyList<string> ErroresDePublicacion(IEnumerable<CriterioRubrica> criterios)
    {
        var lista = criterios.ToList();
        var errores = new List<string>();

        if (lista.Count is < MinimoCriterios or > MaximoCriterios)
        {
            errores.Add($"La rúbrica debe tener entre {MinimoCriterios} y {MaximoCriterios} criterios.");
        }

        if (lista.Any(c => c.Peso <= 0))
        {
            errores.Add("Todos los pesos deben ser positivos.");
        }

        if (lista.Any(c => c.UmbralCumplimiento is < 0 or > 100))
        {
            errores.Add("Los umbrales de cumplimiento deben estar entre 0 y 100.");
        }

        var suma = lista.Sum(c => c.Peso);
        if (suma != SumaPesosRequerida)
        {
            errores.Add($"Los pesos deben sumar exactamente 100 (suman {suma:0.##}).");
        }

        var duplicados = NombresDuplicados(lista.Select(c => c.Nombre));
        if (duplicados.Count > 0)
        {
            errores.Add($"Hay nombres de criterio duplicados: {string.Join(", ", duplicados)}.");
        }

        return errores;
    }

    /// <summary>Comparación sin distinguir mayúsculas ni espacios sobrantes.</summary>
    public static IReadOnlyList<string> NombresDuplicados(IEnumerable<string> nombres) =>
        nombres
            .Select(n => n.Trim())
            .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
}

/// <summary>Tabla criterio_rubrica.</summary>
public class CriterioRubrica : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid RubricaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Peso { get; set; }
    public bool EsCritico { get; set; }
    public decimal UmbralCumplimiento { get; set; }
}
