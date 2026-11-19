using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qualia.Application.Features.Llamadas;
using Qualia.Application.Features.Llamadas.Dtos;

namespace Qualia.Infrastructure.Audio;

/// URLs temporales firmadas con HMAC-SHA256: {urlBase}/api/audio-temporal/{token}, donde
/// token = empresa.llamada.expiracionUnix.firma. Permite que un audio del navegador reproduzca
/// el archivo sin enviar el JWT, sin exponer la ruta interna y solo durante la vigencia indicada.
public sealed class GeneradorUrlAudioFirmada : IGeneradorUrlAudio
{
    public const string RutaBase = "/api/audio-temporal/";

    private readonly byte[] _clave;
    private readonly OpcionesAudio _opciones;
    private readonly TimeProvider _reloj;

    public GeneradorUrlAudioFirmada(IOptions<OpcionesAudio> opciones, TimeProvider reloj, ILogger<GeneradorUrlAudioFirmada> logger)
    {
        _opciones = opciones.Value;
        _reloj = reloj;

        if (string.IsNullOrWhiteSpace(_opciones.ClaveFirma) || _opciones.ClaveFirma.Length < 32)
        {
            logger.LogWarning("Audio:ClaveFirma no está configurada (mín. 32 caracteres); se usará una clave aleatoria temporal.");
            _clave = RandomNumberGenerator.GetBytes(32);
        }
        else
        {
            _clave = Encoding.UTF8.GetBytes(_opciones.ClaveFirma);
        }
    }

    public AccesoAudioDto Generar(Guid empresaId, Guid llamadaId, string urlBase)
    {
        var expira = _reloj.GetUtcNow().Add(_opciones.VigenciaUrl);
        var carga = $"{empresaId:N}.{llamadaId:N}.{expira.ToUnixTimeSeconds()}";
        var token = $"{carga}.{Firmar(carga)}";

        return new AccesoAudioDto($"{urlBase.TrimEnd('/')}{RutaBase}{token}", expira.UtcDateTime);
    }

    public bool Validar(string token, out Guid empresaId, out Guid llamadaId)
    {
        empresaId = Guid.Empty;
        llamadaId = Guid.Empty;

        var partes = token.Split('.');
        if (partes.Length != 4
            || !Guid.TryParseExact(partes[0], "N", out var empresa)
            || !Guid.TryParseExact(partes[1], "N", out var llamada)
            || !long.TryParse(partes[2], out var expiraUnix))
        {
            return false;
        }

        var esperada = Encoding.ASCII.GetBytes(Firmar($"{partes[0]}.{partes[1]}.{partes[2]}"));
        var recibida = Encoding.ASCII.GetBytes(partes[3]);
        if (!CryptographicOperations.FixedTimeEquals(esperada, recibida))
        {
            return false;
        }

        if (_reloj.GetUtcNow().ToUnixTimeSeconds() > expiraUnix)
        {
            return false;
        }

        empresaId = empresa;
        llamadaId = llamada;
        return true;
    }

    private string Firmar(string carga)
    {
        var firma = HMACSHA256.HashData(_clave, Encoding.UTF8.GetBytes(carga));
        return Convert.ToBase64String(firma).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
