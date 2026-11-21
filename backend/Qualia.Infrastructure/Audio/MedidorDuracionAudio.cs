using System.Buffers.Binary;
using Qualia.Application.Features.Llamadas;

namespace Qualia.Infrastructure.Audio;

/// Mide la duración leyendo solo cabeceras y marcos, sin decodificar el audio ni depender de ffmpeg.
/// WhatsApp graba notas de voz en OGG/Opus (.opus u .ogg); también se admiten WAV, MP3 y M4A.
public sealed class MedidorDuracionAudio : IMedidorDuracionAudio
{
    public decimal? Medir(ReadOnlySpan<byte> contenido, string extension)
    {
        try
        {
            var segundos = extension switch
            {
                ".wav" => MedirWav(contenido),
                ".ogg" or ".opus" => MedirOgg(contenido),
                ".mp3" => MedirMp3(contenido),
                ".m4a" => MedirMp4(contenido),
                _ => null,
            };

            return segundos is > 0 and < 1e7 ? Math.Round((decimal)segundos.Value, 3) : null;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            // Cabecera truncada: se trata como archivo dañado.
            return null;
        }
    }

    /// RIFF: bytes del bloque "data" divididos entre los bytes por segundo del bloque "fmt ".
    private static double? MedirWav(ReadOnlySpan<byte> d)
    {
        if (d.Length < 12 || !d[..4].SequenceEqual("RIFF"u8) || !d.Slice(8, 4).SequenceEqual("WAVE"u8))
        {
            return null;
        }

        uint bytesPorSegundo = 0;
        var pos = 12;
        while (pos + 8 <= d.Length)
        {
            var id = d.Slice(pos, 4);
            var tamano = BinaryPrimitives.ReadUInt32LittleEndian(d.Slice(pos + 4, 4));
            var datos = pos + 8;

            if (id.SequenceEqual("fmt "u8))
            {
                bytesPorSegundo = BinaryPrimitives.ReadUInt32LittleEndian(d.Slice(datos + 8, 4));
            }
            else if (id.SequenceEqual("data"u8))
            {
                // Algunos grabadores dejan el tamaño en 0 o 0xFFFFFFFF: se usa lo que realmente hay.
                var disponibles = (uint)(d.Length - datos);
                var bytesAudio = tamano == 0 || tamano > disponibles ? disponibles : tamano;
                return bytesPorSegundo == 0 ? null : (double)bytesAudio / bytesPorSegundo;
            }

            pos = datos + (int)Math.Min(tamano + (tamano & 1), int.MaxValue - datos);
        }

        return null;
    }

    /// OGG: la posición (granule) de la última página del flujo dividida entre la frecuencia.
    /// En Opus la frecuencia es siempre 48 kHz y se descuenta el pre-skip de OpusHead.
    private static double? MedirOgg(ReadOnlySpan<byte> d)
    {
        double frecuencia = 0;
        long preSkip = 0;
        uint? serie = null;
        long ultimaPosicion = -1;

        var pos = 0;
        while (pos + 27 <= d.Length && d.Slice(pos, 4).SequenceEqual("OggS"u8))
        {
            var granule = BinaryPrimitives.ReadInt64LittleEndian(d.Slice(pos + 6, 8));
            var serieActual = BinaryPrimitives.ReadUInt32LittleEndian(d.Slice(pos + 14, 4));
            int segmentos = d[pos + 26];
            var cuerpo = pos + 27 + segmentos;
            var largo = 0;
            for (var i = 0; i < segmentos; i++)
            {
                largo += d[pos + 27 + i];
            }

            if (serie is null)
            {
                serie = serieActual;
                var paquete = d.Slice(cuerpo, Math.Min(largo, d.Length - cuerpo));
                if (paquete.Length >= 19 && paquete[..8].SequenceEqual("OpusHead"u8))
                {
                    frecuencia = 48000;
                    preSkip = BinaryPrimitives.ReadUInt16LittleEndian(paquete.Slice(10, 2));
                }
                else if (paquete.Length >= 16 && paquete[0] == 1 && paquete.Slice(1, 6).SequenceEqual("vorbis"u8))
                {
                    frecuencia = BinaryPrimitives.ReadUInt32LittleEndian(paquete.Slice(12, 4));
                }
                else
                {
                    return null;
                }
            }
            else if (serieActual == serie && granule >= 0)
            {
                ultimaPosicion = granule;
            }

            pos = cuerpo + largo;
        }

        return frecuencia > 0 && ultimaPosicion > preSkip ? (ultimaPosicion - preSkip) / frecuencia : null;
    }

    private static readonly int[][] TasasMp3 =
    [
        [0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448], // MPEG-1 capa I
        [0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384],    // MPEG-1 capa II
        [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320],     // MPEG-1 capa III
        [0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256],    // MPEG-2/2.5 capa I
        [0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160],         // MPEG-2/2.5 capas II y III
    ];

    private static readonly int[][] FrecuenciasMp3 = [[11025, 12000, 8000], [], [22050, 24000, 16000], [44100, 48000, 32000]];

    /// MP3: recorre los marcos y suma sus muestras. Funciona con tasa fija y variable.
    private static double? MedirMp3(ReadOnlySpan<byte> d)
    {
        var pos = 0;
        if (d.Length >= 10 && d[..3].SequenceEqual("ID3"u8))
        {
            var tamano = (d[6] & 0x7F) << 21 | (d[7] & 0x7F) << 14 | (d[8] & 0x7F) << 7 | (d[9] & 0x7F);
            pos = 10 + tamano + ((d[5] & 0x10) != 0 ? 10 : 0);
        }

        double segundos = 0;
        var marcos = 0;
        while (pos + 4 <= d.Length)
        {
            if (d[pos] != 0xFF || (d[pos + 1] & 0xE0) != 0xE0)
            {
                // Se tolera basura entre marcos solo antes del primero (relleno tras ID3).
                if (marcos > 0)
                {
                    break;
                }

                pos++;
                continue;
            }

            var version = (d[pos + 1] >> 3) & 3;     // 0: 2.5, 2: 2, 3: 1
            var capa = (d[pos + 1] >> 1) & 3;        // 1: III, 2: II, 3: I
            var indiceTasa = d[pos + 2] >> 4;
            var indiceFrecuencia = (d[pos + 2] >> 2) & 3;
            var relleno = (d[pos + 2] >> 1) & 1;

            if (version == 1 || capa == 0 || indiceTasa is 0 or 15 || indiceFrecuencia == 3)
            {
                if (marcos > 0)
                {
                    break;
                }

                pos++;
                continue;
            }

            var mpeg1 = version == 3;
            var tabla = (mpeg1, capa) switch
            {
                (true, 3) => 0,
                (true, 2) => 1,
                (true, _) => 2,
                (false, 3) => 3,
                _ => 4,
            };
            var tasa = TasasMp3[tabla][indiceTasa] * 1000;
            var frecuencia = FrecuenciasMp3[version][indiceFrecuencia];

            int muestras;
            int largo;
            if (capa == 3)
            {
                muestras = 384;
                largo = (12 * tasa / frecuencia + relleno) * 4;
            }
            else
            {
                muestras = capa == 1 && !mpeg1 ? 576 : 1152;
                largo = muestras / 8 * tasa / frecuencia + relleno;
            }

            segundos += (double)muestras / frecuencia;
            marcos++;
            pos += largo;
        }

        return marcos > 0 ? segundos : null;
    }

    /// MP4/M4A: duración y escala de tiempo de la caja mvhd dentro de moov.
    private static double? MedirMp4(ReadOnlySpan<byte> d)
    {
        var moov = BuscarCaja(d, "moov"u8);
        if (moov.IsEmpty)
        {
            return null;
        }

        var mvhd = BuscarCaja(moov, "mvhd"u8);
        if (mvhd.Length < 20)
        {
            return null;
        }

        var (escala, duracion) = mvhd[0] == 1
            ? (BinaryPrimitives.ReadUInt32BigEndian(mvhd.Slice(20, 4)), BinaryPrimitives.ReadUInt64BigEndian(mvhd.Slice(24, 8)))
            : (BinaryPrimitives.ReadUInt32BigEndian(mvhd.Slice(12, 4)), BinaryPrimitives.ReadUInt32BigEndian(mvhd.Slice(16, 4)));

        return escala == 0 ? null : (double)duracion / escala;
    }

    /// Devuelve el contenido de la primera caja con ese tipo en este nivel.
    private static ReadOnlySpan<byte> BuscarCaja(ReadOnlySpan<byte> d, ReadOnlySpan<byte> tipo)
    {
        var pos = 0;
        while (pos + 8 <= d.Length)
        {
            long tamano = BinaryPrimitives.ReadUInt32BigEndian(d.Slice(pos, 4));
            var cabecera = 8;
            if (tamano == 1)
            {
                tamano = (long)BinaryPrimitives.ReadUInt64BigEndian(d.Slice(pos + 8, 8));
                cabecera = 16;
            }
            else if (tamano == 0)
            {
                tamano = d.Length - pos;
            }

            if (tamano < cabecera || pos + tamano > d.Length)
            {
                return [];
            }

            if (d.Slice(pos + 4, 4).SequenceEqual(tipo))
            {
                return d.Slice(pos + cabecera, (int)tamano - cabecera);
            }

            pos += (int)tamano;
        }

        return [];
    }
}
