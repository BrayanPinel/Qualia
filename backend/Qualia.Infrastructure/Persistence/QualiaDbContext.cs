using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Seguridad;
using Qualia.Domain.Common;
using Qualia.Domain.Entidades;
using Qualia.Domain.Suscripciones;

namespace Qualia.Infrastructure.Persistence;

public sealed class QualiaDbContext(DbContextOptions<QualiaDbContext> options, IContextoEmpresa contextoEmpresa)
    : DbContext(options), IQualiaDbContext
{
    public const string Esquema = "qualia";

    /// <summary>
    /// Empresa usada por los Global Query Filters. EF Core evalúa esta propiedad en cada consulta,
    /// por lo que el mismo modelo sirve para todas las empresas.
    /// </summary>
    public Guid? EmpresaIdActual => contextoEmpresa.EmpresaIdActual;

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Administrador> Administradores => Set<Administrador>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<Campana> Campanas => Set<Campana>();
    public DbSet<UsuarioCampana> UsuariosCampana => Set<UsuarioCampana>();
    public DbSet<Rubrica> Rubricas => Set<Rubrica>();
    public DbSet<CriterioRubrica> CriteriosRubrica => Set<CriterioRubrica>();

    public DbSet<Llamada> Llamadas => Set<Llamada>();
    public DbSet<TrabajoProcesamiento> TrabajosProcesamiento => Set<TrabajoProcesamiento>();
    public DbSet<SegmentoTranscripcion> SegmentosTranscripcion => Set<SegmentoTranscripcion>();
    public DbSet<EvaluacionIA> EvaluacionesIA => Set<EvaluacionIA>();
    public DbSet<DetalleEvaluacion> DetallesEvaluacion => Set<DetalleEvaluacion>();
    public DbSet<EvidenciaCriterio> EvidenciasCriterio => Set<EvidenciaCriterio>();

    public DbSet<RevisionHumana> RevisionesHumanas => Set<RevisionHumana>();
    public DbSet<CriterioRevisado> CriteriosRevisados => Set<CriterioRevisado>();
    public DbSet<EvidenciaRevision> EvidenciasRevision => Set<EvidenciaRevision>();

    // --- planes, suscripciones y consumo ---
    public DbSet<Plan> Planes => Set<Plan>();
    public DbSet<FuncionPlan> FuncionesPlan => Set<FuncionPlan>();
    public DbSet<Suscripcion> Suscripciones => Set<Suscripcion>();
    public DbSet<PeriodoConsumo> PeriodosConsumo => Set<PeriodoConsumo>();
    public DbSet<ConsumoLlamada> ConsumosLlamada => Set<ConsumoLlamada>();
    public DbSet<AuditoriaPlataforma> AuditoriasPlataforma => Set<AuditoriaPlataforma>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Database.BeginTransactionAsync(cancellationToken);

    public void LimpiarSeguimiento() => ChangeTracker.Clear();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Esquema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QualiaDbContext).Assembly);
        AplicarFiltrosPorEmpresa(modelBuilder);
    }

    /// <summary>
    /// Global Query Filter (HasQueryFilter) para TODAS las entidades que implementan IEntidadEmpresa:
    /// e = e.EmpresaId == EmpresaIdActual. Sin empresa en el contexto no se devuelve ninguna fila.
    /// Las entidades nuevas de cualquier rol quedan filtradas automáticamente al implementar la interfaz.
    /// </summary>
    private void AplicarFiltrosPorEmpresa(ModelBuilder modelBuilder)
    {
        foreach (var tipo in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IEntidadEmpresa).IsAssignableFrom(tipo.ClrType) || tipo.BaseType is not null)
            {
                continue;
            }

            var entidad = Expression.Parameter(tipo.ClrType, "e");
            var empresaEntidad = Expression.Convert(
                Expression.Property(entidad, nameof(IEntidadEmpresa.EmpresaId)), typeof(Guid?));
            var empresaContexto = Expression.Property(Expression.Constant(this), nameof(EmpresaIdActual));

            var filtro = Expression.Lambda(Expression.Equal(empresaEntidad, empresaContexto), entidad);
            modelBuilder.Entity(tipo.ClrType).HasQueryFilter(filtro);
        }
    }
}
