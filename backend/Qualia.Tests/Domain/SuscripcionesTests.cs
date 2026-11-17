using Qualia.Domain.Suscripciones;

namespace Qualia.Tests.Domain;

public sealed class SuscripcionesTests
{
    [Fact]
    public void Plan_TieneFuncion_EsInsensibleAMayusculasYEspacios()
    {
        var plan = new Plan(
            PlanesCatalogo.ProId,
            "Pro",
            99m,
            5000m,
            0.04m,
            10,
            null,
            PlanesCatalogo.RetencionDiasMvp);

        plan.AgregarFuncion(CodigosFuncionPlan.Disputas);

        Assert.True(plan.TieneFuncion("DISPUTAS"));
        Assert.True(plan.TieneFuncion("  disputas  "));
        Assert.False(plan.TieneFuncion("INEXISTENTE"));
        Assert.False(plan.TieneFuncion("   "));
        Assert.True(CodigosFuncionPlan.EsValido("disputas"));
        Assert.False(CodigosFuncionPlan.EsValido("otra"));
    }

    [Fact]
    public void Suscripcion_PermiteNuevasEvaluaciones_RespetaEstadoYVencimiento()
    {
        var plan = new Plan(
            PlanesCatalogo.BasicoId,
            "Básico",
            29m,
            1000m,
            0.05m,
            2,
            1,
            365);

        var empresaId = Guid.NewGuid();
        var registroUtc = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        var suscripcion = Suscripcion.CrearPrueba(empresaId, plan, registroUtc);

        Assert.True(suscripcion.PermiteNuevasEvaluaciones(new DateTime(2026, 11, 30, 23, 59, 59, DateTimeKind.Utc)));
        Assert.False(suscripcion.PermiteNuevasEvaluaciones(new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)));

        Assert.True(suscripcion.RegistrarPagoRechazado(new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(EstadoSuscripcion.EnMora, suscripcion.Estado);
        Assert.False(suscripcion.PermiteNuevasEvaluaciones(new DateTime(2026, 12, 2, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void Suscripcion_CambioPlanYActualizacionCondiciones_ValidanInvariantes()
    {
        var basico = new Plan(PlanesCatalogo.BasicoId, "Básico", 29m, 1000m, 0.05m, 2, 1, 365);
        var pro = new Plan(PlanesCatalogo.ProId, "Pro", 99m, 5000m, 0.04m, 10, null, 365);
        var empresaId = Guid.NewGuid();
        var inicio = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

        var anterior = Suscripcion.CrearPrueba(empresaId, basico, inicio);
        anterior.CerrarPorCambioDePlan();

        Assert.Equal(EstadoSuscripcion.Vencida, anterior.Estado);
        Assert.False(anterior.EsVigente);

        var nueva = Suscripcion.CrearPorCambioDePlan(
            empresaId,
            pro,
            EstadoSuscripcion.Activa,
            inicio,
            inicio.AddMonths(1),
            permiteExcedente: true);

        Assert.True(nueva.EsVigente);
        Assert.True(nueva.PermiteExcedente);

        Assert.Throws<ArgumentException>(() =>
            nueva.ActualizarCondicionesComerciales(
                EstadoSuscripcion.Vencida,
                inicio.AddMonths(1),
                permiteExcedente: false,
                graciaHastaUtc: inicio.AddDays(15)));
    }

    [Fact]
    public void ConsumoLlamada_RedondeoHaciaArribaYEstadosSonIdempotentes()
    {
        Assert.Equal(1.02m, ConsumoLlamada.CalcularMinutos(61m));
        Assert.Equal(1.00m, ConsumoLlamada.CalcularMinutos(60m));

        var plan = new Plan(PlanesCatalogo.BasicoId, "Básico", 29m, 1000m, 0.05m, 2, 1, 365);
        var inicio = new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
        var suscripcion = Suscripcion.CrearPrueba(Guid.NewGuid(), plan, inicio);
        var periodo = PeriodoConsumo.Abrir(suscripcion, PeriodoMensual.DesdeFecha(inicio), plan.MinutosIncluidos);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConsumoLlamada.Reservar(periodo, Guid.NewGuid(), 0m));

        var consumo = ConsumoLlamada.Reservar(periodo, Guid.NewGuid(), 61m);
        Assert.Equal(EstadoConsumo.Reservado, consumo.Estado);
        Assert.Equal(1.02m, consumo.MinutosReservados);
        Assert.Equal(0m, consumo.MinutosFacturables);

        consumo.AcumularCostoTecnico(100, 200, 50, 0.0045m);
        consumo.Facturar();
        consumo.Facturar();

        Assert.Equal(EstadoConsumo.Facturado, consumo.Estado);
        Assert.Equal(0m, consumo.MinutosReservados);
        Assert.Equal(1.02m, consumo.MinutosFacturables);
        Assert.Throws<InvalidOperationException>(() => consumo.Liberar());
    }

    [Fact]
    public void SaldoConsumo_CalculaDisponiblesExcedentesYPermisoReserva()
    {
        var saldo = new SaldoConsumo(MinutosContratados: 100m, MinutosReservados: 30m, MinutosFacturables: 65m);

        Assert.Equal(5m, saldo.MinutosDisponibles);
        Assert.Equal(0m, saldo.MinutosExcedentes);
        Assert.True(saldo.AdmiteReserva(5m, permiteExcedente: false));
        Assert.False(saldo.AdmiteReserva(5.01m, permiteExcedente: false));
        Assert.True(saldo.AdmiteReserva(50m, permiteExcedente: true));

        var saldoConExcedente = new SaldoConsumo(100m, 0m, 125m);
        Assert.Equal(0m, saldoConExcedente.MinutosDisponibles);
        Assert.Equal(25m, saldoConExcedente.MinutosExcedentes);
    }
}
