using Qualia.Application.Features.Procesamiento;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Tests.Unitarias;

public class ConstructorEvaluacionTests
{
    private static readonly Guid Empresa = Guid.NewGuid();
    private static readonly Guid Llamada = Guid.NewGuid();
    private static readonly DateTime Ahora = new(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);

    private readonly CriterioRubrica _saludo = Criterio("Saludo", 40, critico: false, umbral: 70);
    private readonly CriterioRubrica _identidad = Criterio("Identidad", 60, critico: true, umbral: 80);
    private readonly SegmentoTranscripcion _seg1 = Segmento(1, "Buenos días, gracias por llamar, le atiende Sofía.");
    private readonly SegmentoTranscripcion _seg2 = Segmento(2, "Para verificar su identidad, ¿me confirma su número?");

    [Fact]
    public void Construye_evaluacion_valida_con_evidencias_y_cumplimiento_calculado()
    {
        var respuesta = Respuesta(
            new CriterioEvaluado(_saludo.Id, 90, "Saluda correctamente.", [new(_seg1.Id, "gracias por llamar")]),
            new CriterioEvaluado(_identidad.Id, 75, "Verifica parcialmente.", [new(_seg2.Id, "  PARA verificar su   identidad ")]));

        var resultado = Construir(respuesta);

        var evaluacion = resultado.Evaluacion;
        Assert.Equal(0, resultado.EvidenciasDescartadas);
        Assert.False(evaluacion.RequiereRevision);
        Assert.Equal(EstadoEvaluacion.EvaluadaPorIA, evaluacion.Estado);
        Assert.Equal(1, evaluacion.Revision);
        Assert.Equal(2, evaluacion.Detalles.Count);

        var saludo = evaluacion.Detalles.Single(d => d.CriterioId == _saludo.Id);
        Assert.True(saludo.CumpleCriterio);
        Assert.Single(saludo.Evidencias);

        var identidad = evaluacion.Detalles.Single(d => d.CriterioId == _identidad.Id);
        Assert.False(identidad.CumpleCriterio); // 75 < umbral 80: el cumplimiento lo decide el servidor.
    }

    [Fact]
    public void Evidencia_de_segmento_ajeno_a_la_llamada_se_descarta_y_exige_revision()
    {
        var segmentoDeOtraLlamada = Guid.NewGuid();
        var respuesta = Respuesta(
            new CriterioEvaluado(_saludo.Id, 90, "Saluda.", [new(segmentoDeOtraLlamada, "gracias por llamar")]),
            new CriterioEvaluado(_identidad.Id, 85, "Verifica.", [new(_seg2.Id, "verificar su identidad")]));

        var resultado = Construir(respuesta);

        Assert.Equal(1, resultado.EvidenciasDescartadas);
        Assert.True(resultado.Evaluacion.RequiereRevision);
        var saludo = resultado.Evaluacion.Detalles.Single(d => d.CriterioId == _saludo.Id);
        Assert.Empty(saludo.Evidencias);
        Assert.Contains("requiere revisión humana", saludo.Observacion);
    }

    [Fact]
    public void Cita_que_no_existe_en_el_segmento_se_descarta()
    {
        var respuesta = Respuesta(
            new CriterioEvaluado(_saludo.Id, 90, "Saluda.", [new(_seg1.Id, "texto inventado por el modelo")]),
            new CriterioEvaluado(_identidad.Id, 85, "Verifica.", []));

        var resultado = Construir(respuesta);

        Assert.True(resultado.Evaluacion.RequiereRevision);
        Assert.All(resultado.Evaluacion.Detalles, d => Assert.Empty(d.Evidencias));
    }

    [Fact]
    public void Ausencia_de_evidencia_se_representa_explicitamente_sin_exigir_revision()
    {
        var respuesta = Respuesta(
            new CriterioEvaluado(_saludo.Id, 40, "No saluda.", []),
            new CriterioEvaluado(_identidad.Id, 85, "Verifica.", [new(_seg2.Id, "verificar su identidad")]));

        var resultado = Construir(respuesta);

        Assert.False(resultado.Evaluacion.RequiereRevision);
        var saludo = resultado.Evaluacion.Detalles.Single(d => d.CriterioId == _saludo.Id);
        Assert.Empty(saludo.Evidencias);
        Assert.Contains(ConstructorEvaluacion.NotaSinEvidencia, saludo.Observacion);
    }

    [Fact]
    public void Falta_un_criterio_de_la_rubrica_lanza_error_reintentable()
    {
        var respuesta = Respuesta(new CriterioEvaluado(_saludo.Id, 90, "Saluda.", []));

        var error = Assert.Throws<ErrorProcesamiento>(() => Construir(respuesta));
        Assert.IsNotType<ErrorProcesamientoPermanente>(error);
    }

    [Fact]
    public void Criterio_que_no_pertenece_a_la_rubrica_lanza_error()
    {
        var respuesta = Respuesta(
            new CriterioEvaluado(_saludo.Id, 90, "Saluda.", []),
            new CriterioEvaluado(Guid.NewGuid(), 90, "Otro.", []));

        Assert.Throws<ErrorProcesamiento>(() => Construir(respuesta));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    public void Puntaje_fuera_de_rango_lanza_error(double puntaje)
    {
        var respuesta = Respuesta(
            new CriterioEvaluado(_saludo.Id, (decimal)puntaje, "x", []),
            new CriterioEvaluado(_identidad.Id, 50, "x", []));

        Assert.Throws<ErrorProcesamiento>(() => Construir(respuesta));
    }

    [Fact]
    public void Citas_repetidas_del_mismo_segmento_se_guardan_una_vez()
    {
        var respuesta = Respuesta(
            new CriterioEvaluado(_saludo.Id, 90, "Saluda.", [new(_seg1.Id, "Buenos días"), new(_seg1.Id, "gracias por llamar")]),
            new CriterioEvaluado(_identidad.Id, 85, "Verifica.", []));

        var saludo = Construir(respuesta).Evaluacion.Detalles.Single(d => d.CriterioId == _saludo.Id);

        Assert.Single(saludo.Evidencias);
    }

    private ConstructorEvaluacion.Resultado Construir(ResultadoEvaluacionIA respuesta) =>
        ConstructorEvaluacion.Construir(Empresa, Llamada, respuesta, [_saludo, _identidad], [_seg1, _seg2], Ahora);

    private static ResultadoEvaluacionIA Respuesta(params CriterioEvaluado[] criterios) =>
        new("modelo-prueba", ModoIA.Simulado, criterios, 10, 10, 0m);

    private static CriterioRubrica Criterio(string nombre, decimal peso, bool critico, decimal umbral) => new()
    {
        EmpresaId = Empresa, Nombre = nombre, Descripcion = nombre, Peso = peso, EsCritico = critico, UmbralCumplimiento = umbral,
    };

    private static SegmentoTranscripcion Segmento(int orden, string texto) => new()
    {
        EmpresaId = Empresa, LlamadaId = Llamada, Orden = orden, Hablante = "Agente", InicioSeg = orden * 10, FinSeg = orden * 10 + 5, Texto = texto,
    };
}
