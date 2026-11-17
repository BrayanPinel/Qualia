using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qualia.Domain.Suscripciones;

namespace Qualia.Infrastructure.Persistence.Configurations.Suscripciones;

public sealed class FuncionPlanConfiguration : IEntityTypeConfiguration<FuncionPlan>
{
    public void Configure(EntityTypeBuilder<FuncionPlan> builder)
    {
        builder.ToTable("funcion_plan");

        builder.HasKey(f => new { f.PlanId, f.CodigoFuncion });

        builder.Property(f => f.PlanId)
            .HasColumnName("plan_id")
            .IsRequired();

        builder.Property(f => f.CodigoFuncion)
            .HasColumnName("codigo_funcion")
            .HasMaxLength(FuncionPlan.LongitudMaximaCodigo)
            .IsRequired();

        builder.HasData(
            // Plan Básico
            new { PlanId = PlanesCatalogo.BasicoId, CodigoFuncion = CodigosFuncionPlan.Evaluacion },
            new { PlanId = PlanesCatalogo.BasicoId, CodigoFuncion = CodigosFuncionPlan.ScorecardBasico },

            // Plan Pro
            new { PlanId = PlanesCatalogo.ProId, CodigoFuncion = CodigosFuncionPlan.Evaluacion },
            new { PlanId = PlanesCatalogo.ProId, CodigoFuncion = CodigosFuncionPlan.ScorecardBasico },
            new { PlanId = PlanesCatalogo.ProId, CodigoFuncion = CodigosFuncionPlan.Tendencia },
            new { PlanId = PlanesCatalogo.ProId, CodigoFuncion = CodigosFuncionPlan.ComparacionCampanas },
            new { PlanId = PlanesCatalogo.ProId, CodigoFuncion = CodigosFuncionPlan.Disputas },

            // Plan Enterprise
            new { PlanId = PlanesCatalogo.EnterpriseId, CodigoFuncion = CodigosFuncionPlan.Evaluacion },
            new { PlanId = PlanesCatalogo.EnterpriseId, CodigoFuncion = CodigosFuncionPlan.ScorecardBasico },
            new { PlanId = PlanesCatalogo.EnterpriseId, CodigoFuncion = CodigosFuncionPlan.Tendencia },
            new { PlanId = PlanesCatalogo.EnterpriseId, CodigoFuncion = CodigosFuncionPlan.ComparacionCampanas },
            new { PlanId = PlanesCatalogo.EnterpriseId, CodigoFuncion = CodigosFuncionPlan.Disputas });
    }
}
