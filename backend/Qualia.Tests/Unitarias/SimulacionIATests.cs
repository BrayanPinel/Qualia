using Qualia.Application.Features.Procesamiento;
using Qualia.Domain.Enums;
using Qualia.Infrastructure.Procesamiento;

namespace Qualia.Tests.Unitarias;

public class SimulacionIATests
{
    private static readonly Guid Empresa = Guid.NewGuid();

    [Fact]
    public async Task Transcripcion_simulada_es_reproducible_para_la_misma_llamada()
    {
        var transcriptor = new TranscriptorSimulado();
        var solicitud = new SolicitudTranscripcion(Empresa, Guid.Parse("10000000-0000-0000-0005-000000000001"), "x.wav", 182.5m);

        var a = await transcriptor.TranscribirAsync(solicitud, CancellationToken.None);
        var b = await transcriptor.TranscribirAsync(solicitud, CancellationToken.None);

        Assert.Equal(a.Segmentos, b.Segmentos);
    }

    [Theory]
    [InlineData(95)]
    [InlineData(310.75)]
    public async Task Segmentos_son_contiguos_y_quedan_dentro_de_la_duracion(double duracion)
    {
        var dur = (decimal)duracion;
        var resultado = await new TranscriptorSimulado().TranscribirAsync(
            new SolicitudTranscripcion(Empresa, Guid.NewGuid(), "x.wav", dur), CancellationToken.None);

        Assert.NotEmpty(resultado.Segmentos);
        Assert.Equal(0m, resultado.Segmentos[0].InicioSeg);
        Assert.Equal(dur, resultado.Segmentos[^1].FinSeg);
        for (var i = 1; i < resultado.Segmentos.Count; i++)
        {
            Assert.Equal(resultado.Segmentos[i - 1].FinSeg, resultado.Segmentos[i].InicioSeg);
            Assert.True(resultado.Segmentos[i].FinSeg >= resultado.Segmentos[i].InicioSeg);
        }
    }

    [Fact]
    public async Task Evaluacion_simulada_es_reproducible_y_cita_texto_literal_del_segmento()
    {
        var llamada = Guid.NewGuid();
        var segmentos = new List<SegmentoParaEvaluar>
        {
            new(Guid.NewGuid(), 1, "Agente", "Buenos días, gracias por llamar a Qualia Servicios, le saluda su asesor. ¿Con quién tengo el gusto?"),
            new(Guid.NewGuid(), 2, "Cliente", "Hola, quiero consultar un cobro."),
            new(Guid.NewGuid(), 3, "Agente", "Para verificar su identidad, ¿me confirma su número de identidad?"),
        };
        var criterios = new List<CriterioParaEvaluar>
        {
            new(Guid.NewGuid(), "Saludo y presentación", "El agente saluda y se presenta.", 50, false, 70),
            new(Guid.NewGuid(), "Verificación de identidad", "El agente verifica la identidad del cliente.", 50, true, 80),
        };
        var solicitud = new SolicitudEvaluacion(Empresa, llamada, criterios, segmentos);
        var evaluador = new EvaluadorIASimulado();

        var a = await evaluador.EvaluarAsync(solicitud, CancellationToken.None);
        var b = await evaluador.EvaluarAsync(solicitud, CancellationToken.None);

        Assert.Equal(ModoIA.Simulado, a.Modo);
        Assert.Equal(a.Criterios.Select(c => c.Puntaje), b.Criterios.Select(c => c.Puntaje));
        Assert.All(a.Criterios.SelectMany(c => c.Evidencias), e =>
        {
            var segmento = segmentos.Single(s => s.SegmentoId == e.SegmentoId);
            Assert.Contains(e.CitaTextual, segmento.Texto);
        });

        var verificacion = a.Criterios.Single(c => c.CriterioId == criterios[1].CriterioId);
        Assert.Equal(segmentos[2].SegmentoId, Assert.Single(verificacion.Evidencias).SegmentoId);
    }
}
