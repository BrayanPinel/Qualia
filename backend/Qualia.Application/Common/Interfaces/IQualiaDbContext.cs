using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qualia.Domain.Entidades;

namespace Qualia.Application.Common.Interfaces;

/// <summary>
/// Abstracción del contexto de datos para la capa de Aplicación.
/// La implementación (EF Core + PostgreSQL) vive en Infrastructure.
/// Cada rol agrega aquí los DbSet de sus propias entidades.
/// </summary>
public interface IQualiaDbContext
{
    // --- Entidades base compartidas---
    DbSet<Empresa> Empresas { get; }
    DbSet<Usuario> Usuarios { get; }
    DbSet<Campana> Campanas { get; }
    DbSet<UsuarioCampana> UsuariosCampana { get; }
    DbSet<Rubrica> Rubricas { get; }
    DbSet<CriterioRubrica> CriteriosRubrica { get; }

    // --- llamadas y procesamiento ---
    DbSet<Llamada> Llamadas { get; }
    DbSet<TrabajoProcesamiento> TrabajosProcesamiento { get; }
    DbSet<SegmentoTranscripcion> SegmentosTranscripcion { get; }
    DbSet<EvaluacionIA> EvaluacionesIA { get; }
    DbSet<DetalleEvaluacion> DetallesEvaluacion { get; }
    DbSet<EvidenciaCriterio> EvidenciasCriterio { get; }

    // --- revisión humana ---
    DbSet<RevisionHumana> RevisionesHumanas { get; }
    DbSet<CriterioRevisado> CriteriosRevisados { get; }
    DbSet<EvidenciaRevision> EvidenciasRevision { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Abre una transacción en la conexión de este contexto.</summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Descarta las entidades rastreadas .</summary>
    void LimpiarSeguimiento();
}
