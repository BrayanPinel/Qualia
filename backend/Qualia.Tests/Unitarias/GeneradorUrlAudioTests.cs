using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Qualia.Infrastructure.Audio;

namespace Qualia.Tests.Unitarias;

public class GeneradorUrlAudioTests
{
    private readonly RelojManual _reloj = new(new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero));
    private readonly GeneradorUrlAudioFirmada _generador;

    public GeneradorUrlAudioTests()
    {
        var opciones = Options.Create(new OpcionesAudio
        {
            ClaveFirma = "clave-de-prueba-con-mas-de-32-caracteres!!",
            VigenciaUrl = TimeSpan.FromMinutes(10),
        });
        _generador = new GeneradorUrlAudioFirmada(opciones, _reloj, NullLogger<GeneradorUrlAudioFirmada>.Instance);
    }

    [Fact]
    public void Url_valida_no_expone_la_ruta_interna_y_conserva_empresa_y_llamada()
    {
        var empresa = Guid.NewGuid();
        var llamada = Guid.NewGuid();

        var acceso = _generador.Generar(empresa, llamada, "https://api.qualia.test/");

        Assert.StartsWith("https://api.qualia.test/api/audio-temporal/", acceso.Url);
        Assert.DoesNotContain(".wav", acceso.Url);
        Assert.Equal(_reloj.GetUtcNow().AddMinutes(10).UtcDateTime, acceso.ExpiraEn);

        Assert.True(_generador.Validar(Token(acceso.Url), out var e, out var l));
        Assert.Equal(empresa, e);
        Assert.Equal(llamada, l);
    }

    [Fact]
    public void Token_alterado_se_rechaza()
    {
        var token = Token(_generador.Generar(Guid.NewGuid(), Guid.NewGuid(), "http://x").Url);
        var otraEmpresa = Guid.NewGuid().ToString("N");
        var alterado = otraEmpresa + token[32..];

        Assert.False(_generador.Validar(alterado, out _, out _));
        Assert.False(_generador.Validar("basura", out _, out _));
    }

    [Fact]
    public void Token_expirado_se_rechaza()
    {
        var token = Token(_generador.Generar(Guid.NewGuid(), Guid.NewGuid(), "http://x").Url);

        _reloj.Avanzar(TimeSpan.FromMinutes(11));

        Assert.False(_generador.Validar(token, out _, out _));
    }

    private static string Token(string url) => url[(url.LastIndexOf('/') + 1)..];

    private sealed class RelojManual(DateTimeOffset inicio) : TimeProvider
    {
        private DateTimeOffset _ahora = inicio;
        public override DateTimeOffset GetUtcNow() => _ahora;
        public void Avanzar(TimeSpan t) => _ahora += t;
    }
}
