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
    public void Criterio_cumplido_sin_evidencia_se_representa_explicitamente_sin_exigir_revision()
    {
        var respuesta = Respuesta(
            new CriterioEvaluado(_saludo.Id, 90, "Saluda.", []),
            new CriterioEvaluado(_identidad.Id, 95, "Verifica.", [new(_seg2.Id, "verificar su identidad")]));

        var resultado = Construir(respuesta);

        Assert.False(resultado.Evaluacion.RequiereRevision);
        Assert.True(resultado.Evaluacion.EsOficial);
        var saludo = resultado.Evaluacion.Detalles.Single(d => d.CriterioId == _saludo.Id);
        Assert.Empty(saludo.Evidencias);
        Assert.Contains(ConstructorEvaluacion.NotaSinEvidencia, saludo.Observacion);
    }

    [Fact]
    public void Falta_sin_fragmento_que_la_pruebe_exige_revision()
    {
        var respuesta = Respuesta(
            new CriterioEvaluado(_saludo.Id, 40, "No saluda.", []),
            new CriterioEvaluado(_identidad.Id, 95, "Verifica.", [new(_seg2.Id, "verificar su identidad")]));

        var resultado = Construir(respuesta);

        Assert.True(resultado.Evaluacion.RequiereRevision);
        Assert.False(resultado.Evaluacion.EsOficial);
        Assert.Equal(1, resultado.CriteriosEnRevision);
        var saludo = resultado.Evaluacion.Detalles.Single(d => d.CriterioId == _saludo.Id);
        Assert.Contains("falta sin citar el fragmento", saludo.Observacion);
    }

    [Fact]
    public void Falta_con_fragmento_valido_es_oficial_y_conserva_el_fragmento()
    {
        var respuesta = Respuesta(
            new CriterioEvaluado(_saludo.Id, 90, "Saluda.", [new(_seg1.Id, "gracias por llamar")]),
            new CriterioEvaluado(_identidad.Id, 30, "Pide el número sin verificar.", [new(_seg2.Id, "me confirma su número")]));

        var resultado = Construir(respuesta, margen: 5m);

        Assert.False(resultado.Evaluacion.RequiereRevision);
        Assert.True(resultado.Evaluacion.EsOficial);
        var identidad = resultado.Evaluacion.Detalles.Single(d => d.CriterioId == _identidad.Id);
        Assert.False(identidad.CumpleCriterio);
        Assert.Equal(_seg2.Id, Assert.Single(identidad.Evidencias).SegmentoId);
    }

    [Theory]
    [InlineData(76, true)]   // umbral 80: a 4 puntos, dudosa.
    [InlineData(84, true)]
    [InlineData(75, false)]  // a 5 puntos: fuera del margen.
    [InlineData(95, false)]
    public void Nota_cerca_del_umbral_exige_revision(double puntaje, bool esperado)
    {
        var respuesta = Respuesta(
            new CriterioEvaluado(_saludo.Id, 90, "Saluda.", [new(_seg1.Id, "gracias por llamar")]),
            new CriterioEvaluado(_identidad.Id, (decimal)puntaje, "Verifica.", [new(_seg2.Id, "verificar su identidad")]));

        var resultado = Construir(respuesta, margen: 5m);

        Assert.Equal(esperado, resultado.Evaluacion.RequiereRevision);
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

    private ConstructorEvaluacion.Resultado Construir(ResultadoEvaluacionIA respuesta, decimal margen = 0m) =>
        ConstructorEvaluacion.Construir(Empresa, Llamada, respuesta, [_saludo, _identidad], [_seg1, _seg2], Ahora, margen);

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
