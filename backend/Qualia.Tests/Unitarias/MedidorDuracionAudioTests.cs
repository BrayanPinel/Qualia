using System.Buffers.Binary;
using System.Text;
using Qualia.Infrastructure.Audio;

namespace Qualia.Tests.Unitarias;

/// Los archivos se arman en memoria con la misma estructura que producen los grabadores reales.
public class MedidorDuracionAudioTests
{
    private readonly MedidorDuracionAudio _medidor = new();

    [Fact]
    public void Wav_divide_los_bytes_de_audio_entre_los_bytes_por_segundo()
    {
        Assert.Equal(3m, _medidor.Medir(ArchivosAudio.Wav(segundos: 3), ".wav"));
    }

    [Theory]
    [InlineData(".opus")]
    [InlineData(".ogg")]
    public void Nota_de_voz_de_WhatsApp_en_ogg_opus_descuenta_el_pre_skip(string extension)
    {
        Assert.Equal(5m, _medidor.Medir(ArchivosAudio.OggOpus(segundos: 5), extension));
    }

    [Fact]
    public void Mp3_suma_la_duracion_de_cada_marco()
    {
        // 100 marcos MPEG-1 capa III a 44,1 kHz: 100 × 1152 / 44100 s.
        Assert.Equal(Math.Round(100m * 1152m / 44100m, 3), _medidor.Medir(ArchivosAudio.Mp3(marcos: 100), ".mp3"));
    }

    [Fact]
    public void M4a_usa_la_escala_de_tiempo_de_mvhd()
    {
        Assert.Equal(7.5m, _medidor.Medir(ArchivosAudio.M4a(escala: 1000, duracion: 7500), ".m4a"));
    }

    [Theory]
    [InlineData(".wav")]
    [InlineData(".ogg")]
    [InlineData(".mp3")]
    [InlineData(".m4a")]
    public void Archivo_que_no_es_audio_devuelve_null(string extension)
    {
        Assert.Null(_medidor.Medir(Encoding.UTF8.GetBytes("esto no es un audio, es texto plano"), extension));
    }

    [Fact]
    public void Ogg_truncado_devuelve_null_sin_lanzar()
    {
        var completo = ArchivosAudio.OggOpus(segundos: 5);
        Assert.Null(_medidor.Medir(completo.AsSpan(0, 40), ".ogg"));
    }

    [Fact]
    public void Extension_desconocida_devuelve_null()
    {
        Assert.Null(_medidor.Medir(ArchivosAudio.Wav(segundos: 1), ".flac"));
    }
}

/// Generadores mínimos de cada formato, compartidos con las pruebas de integración.
public static class ArchivosAudio
{
    /// PCM 8 kHz, 16 bits, mono.
    public static byte[] Wav(int segundos)
    {
        const int bytesPorSegundo = 16000;
        var datos = segundos * bytesPorSegundo;
        var b = new byte[44 + datos];
        "RIFF"u8.CopyTo(b);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(4), 36 + datos);
        "WAVEfmt "u8.CopyTo(b.AsSpan(8));
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(16), 16);
        BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(20), 1);
        BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(22), 1);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(24), 8000);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(28), bytesPorSegundo);
        BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(32), 2);
        BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(34), 16);
        "data"u8.CopyTo(b.AsSpan(36));
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(40), datos);
        return b;
    }

    public static byte[] OggOpus(int segundos)
    {
        const ushort preSkip = 312;
        var cabecera = new byte[19];
        "OpusHead"u8.CopyTo(cabecera);
        cabecera[8] = 1;
        cabecera[9] = 1;
        BinaryPrimitives.WriteUInt16LittleEndian(cabecera.AsSpan(10), preSkip);
        BinaryPrimitives.WriteUInt32LittleEndian(cabecera.AsSpan(12), 48000);

        using var salida = new MemoryStream();
        PaginaOgg(salida, granule: 0, cabecera);
        PaginaOgg(salida, granule: 0, Encoding.ASCII.GetBytes("OpusTags........"));
        PaginaOgg(salida, granule: preSkip + 48000L * segundos / 2, new byte[50]);
        PaginaOgg(salida, granule: preSkip + 48000L * segundos, new byte[50]);
        return salida.ToArray();
    }

    private static void PaginaOgg(Stream salida, long granule, byte[] cuerpo)
    {
        var h = new byte[28];
        "OggS"u8.CopyTo(h);
        BinaryPrimitives.WriteInt64LittleEndian(h.AsSpan(6), granule);
        BinaryPrimitives.WriteUInt32LittleEndian(h.AsSpan(14), 0xCAFE);
        h[26] = 1;
        h[27] = (byte)cuerpo.Length;
        salida.Write(h);
        salida.Write(cuerpo);
    }

    /// MPEG-1 capa III, 128 kbps, 44,1 kHz, sin relleno: 417 bytes por marco. Con etiqueta ID3 vacía al inicio.
    public static byte[] Mp3(int marcos)
    {
        using var salida = new MemoryStream();
        salida.Write("ID3"u8);
        salida.Write([3, 0, 0, 0, 0, 0, 0]);
        for (var i = 0; i < marcos; i++)
        {
            var marco = new byte[417];
            marco[0] = 0xFF;
            marco[1] = 0xFB;
            marco[2] = 0x90;
            salida.Write(marco);
        }

        return salida.ToArray();
    }

    public static byte[] M4a(uint escala, uint duracion)
    {
        var mvhd = new byte[8 + 100];
        BinaryPrimitives.WriteUInt32BigEndian(mvhd, (uint)mvhd.Length);
        "mvhd"u8.CopyTo(mvhd.AsSpan(4));
        BinaryPrimitives.WriteUInt32BigEndian(mvhd.AsSpan(8 + 12), escala);
        BinaryPrimitives.WriteUInt32BigEndian(mvhd.AsSpan(8 + 16), duracion);

        var ftyp = new byte[16];
        BinaryPrimitives.WriteUInt32BigEndian(ftyp, 16);
        "ftypM4A "u8.CopyTo(ftyp.AsSpan(4));

        var moov = new byte[8 + mvhd.Length];
        BinaryPrimitives.WriteUInt32BigEndian(moov, (uint)moov.Length);
        "moov"u8.CopyTo(moov.AsSpan(4));
        mvhd.CopyTo(moov.AsSpan(8));

        return [.. ftyp, .. moov];
    }
}
