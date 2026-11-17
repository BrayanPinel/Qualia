using Microsoft.EntityFrameworkCore;
using Qualia.Domain.Suscripciones;

namespace Qualia.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeder idempotente para el catálogo inicial de planes comerciales (Básico,
/// Pro, Enterprise) y sus funciones (funcion_plan).
/// Puede invocarse múltiples veces en el arranque o en pruebas de integración sin
/// duplicar registros.
/// </summary>
public sealed class PlanesSeeder
{
    private readonly QualiaDbContext _dbContext;

    public PlanesSeeder(QualiaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Construye las instancias de dominio del catálogo inicial de planes y funciones.
    /// </summary>
    public static IReadOnlyList<Plan> CrearCatalogoInicial()
    {
        var basico = new Plan(
            id: PlanesCatalogo.BasicoId,
            nombre: "Básico",
            precioMensual: 29.00m,
            minutosIncluidos: 1000.00m,
            precioExcedente: 0.05m,
            limiteSupervisores: 2,
            limiteRubricas: 1,
            retencionDias: PlanesCatalogo.RetencionDiasMvp,
            activo: true);
        basico.AgregarFuncion(CodigosFuncionPlan.Evaluacion);
        basico.AgregarFuncion(CodigosFuncionPlan.ScorecardBasico);

        var pro = new Plan(
            id: PlanesCatalogo.ProId,
            nombre: "Pro",
            precioMensual: 99.00m,
            minutosIncluidos: 5000.00m,
            precioExcedente: 0.04m,
            limiteSupervisores: 10,
            limiteRubricas: null,
            retencionDias: PlanesCatalogo.RetencionDiasMvp,
            activo: true);
        foreach (var codigo in CodigosFuncionPlan.Todos)
        {
            pro.AgregarFuncion(codigo);
        }

        var enterprise = new Plan(
            id: PlanesCatalogo.EnterpriseId,
            nombre: "Enterprise",
            precioMensual: 299.00m,
            minutosIncluidos: 20000.00m,
            precioExcedente: 0.03m,
            limiteSupervisores: null,
            limiteRubricas: null,
            retencionDias: PlanesCatalogo.RetencionDiasMvp,
            activo: true);
        foreach (var codigo in CodigosFuncionPlan.Todos)
        {
            enterprise.AgregarFuncion(codigo);
        }

        return [basico, pro, enterprise];
    }

    /// <summary>
    /// Inserta de forma idempotente los planes iniciales y sus funciones faltantes.
    /// </summary>
    public async Task SembrarAsync(CancellationToken cancellationToken = default)
    {
        var catalogo = CrearCatalogoInicial();
        var existentes = await _dbContext.Planes
            .Include(p => p.Funciones)
            .ToListAsync(cancellationToken);

        var huboCambios = false;

        foreach (var planSemilla in catalogo)
        {
            var existente = existentes.FirstOrDefault(p =>
                p.Id == planSemilla.Id ||
                string.Equals(p.Nombre, planSemilla.Nombre, StringComparison.OrdinalIgnoreCase));

            if (existente is null)
            {
                _dbContext.Planes.Add(planSemilla);
                huboCambios = true;
                continue;
            }

            foreach (var funcion in planSemilla.Funciones)
            {
                if (existente.AgregarFuncion(funcion.CodigoFuncion) is not null)
                {
                    huboCambios = true;
                }
            }
        }

        if (huboCambios)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
