using Qualia.Domain.Common;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;
using Qualia.Domain.Servicios;

namespace Qualia.Tests.Unitarias;

public class DominioTests
{
    private static readonly DateTime Ahora = new(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Trabajo_reintenta_con_espera_exponencial_y_falla_al_agotar_intentos()
    {
        var trabajo = TrabajoProcesamiento.Crear(Guid.NewGuid(), Guid.NewGuid());
        var espera = TimeSpan.FromSeconds(10);

        Reclamar(trabajo);
        Assert.False(trabajo.RegistrarFallo("error 1", maximoIntentos: 3, Ahora, espera));
        Assert.Equal(EstadoTrabajo.ReintentoPendiente, trabajo.Estado);
        Assert.Equal(Ahora.AddSeconds(10), trabajo.ProximoIntento);

        Reclamar(trabajo);
        Assert.False(trabajo.RegistrarFallo("error 2", 3, Ahora, espera));
        Assert.Equal(Ahora.AddSeconds(20), trabajo.ProximoIntento);

        Reclamar(trabajo);
        Assert.True(trabajo.RegistrarFallo("error 3", 3, Ahora, espera));
        Assert.Equal(EstadoTrabajo.Fallido, trabajo.Estado);
        Assert.Equal("error 3", trabajo.UltimoError);
        Assert.Null(trabajo.ProximoIntento);
    }

    [Fact]
    public void Trabajo_solo_cambia_de_estado_si_esta_en_proceso()
    {
        var trabajo = TrabajoProcesamiento.Crear(Guid.NewGuid(), Guid.NewGuid());
        Assert.Throws<ReglaDominioException>(trabajo.Completar);
    }

    [Fact]
    public void Llamada_rechaza_transiciones_invalidas()
    {
        var llamada = new Llamada { Estado = EstadoLlamada.Disponible };

        Assert.Throws<ReglaDominioException>(() => llamada.CambiarEstado(EstadoLlamada.EvaluadaPorIA));

        llamada.CambiarEstado(EstadoLlamada.EnCola);
        llamada.CambiarEstado(EstadoLlamada.Transcribiendo);
        llamada.CambiarEstado(EstadoLlamada.Transcrita);
        llamada.CambiarEstado(EstadoLlamada.Evaluando);
        llamada.CambiarEstado(EstadoLlamada.EvaluadaPorIA);

        Assert.Throws<ReglaDominioException>(() => llamada.CambiarEstado(EstadoLlamada.EnCola));
    }

    [Fact]
    public void Llamada_interrumpida_se_reanuda_en_cola()
    {
        var llamada = new Llamada { Estado = EstadoLlamada.Evaluando };
        llamada.ReanudarEnCola();
        Assert.Equal(EstadoLlamada.EnCola, llamada.Estado);

        var terminada = new Llamada { Estado = EstadoLlamada.EvaluadaPorIA };
        terminada.ReanudarEnCola();
        Assert.Equal(EstadoLlamada.EvaluadaPorIA, terminada.Estado);
    }

    [Fact]
    public void Calculadora_aplica_las_reglas_de_puntaje_de_la_fase_1()
    {
        // 90×20% + 70×30% + 85×30% + 60×20% = 18 + 21 + 25.5 + 12 = 76.5
        Assert.Equal(76.5m, CalculadoraPuntaje.Total([(90, 20), (70, 30), (85, 30), (60, 20)]));
        Assert.Equal(33.33m, CalculadoraPuntaje.Total([(33.333m, 100)]));

        Assert.True(CalculadoraPuntaje.Cumple(80, 80));
        Assert.True(CalculadoraPuntaje.EsFalloCritico(esCritico: true, puntaje: 79.99m, umbralCumplimiento: 80));
        Assert.False(CalculadoraPuntaje.EsFalloCritico(esCritico: false, puntaje: 10, umbralCumplimiento: 80));
    }

    /// <summary>Simula el UPDATE atómico con el que el ejecutor reclama el trabajo.</summary>
    private static void Reclamar(TrabajoProcesamiento trabajo)
    {
        trabajo.Estado = EstadoTrabajo.EnProceso;
        trabajo.Intentos++;
    }
}
