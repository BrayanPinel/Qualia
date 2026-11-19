#:property PublishAot=false

// Generador de audios ficticios y ground data para Qualia.
//
// Uso (desde la carpeta backend/):
//   dotnet run herramientas/GenerarAudiosDemo.cs            -> genera solo los que faltan
//   dotnet run herramientas/GenerarAudiosDemo.cs -- --forzar -> regenera todos
//
// Entrada: Qualia.API/AudiosDemo/guiones/*.json
// Salida:  un .wav y un .ground.json por guion en Qualia.API/AudiosDemo
//
// Cada turno se sintetiza con Deepgram Aura-2 y se concatena con pausas.
// Como el audio se arma por turnos, se sabe qué actor habla en cada segundo.
// La llave se lee de la variable de entorno Deepgram__ApiKey o de Qualia.API/.env.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

const int FrecuenciaMuestreo = 8000;   // calidad telefónica
const int BytesPorMuestra = 2;         // PCM 16 bits
const double SilencioInicial = 0.4;
const double PausaEntreTurnos = 0.45;
const double SilencioFinal = 0.5;

var forzar = args.Contains("--forzar");
var raizApi = BuscarCarpetaApi(Environment.CurrentDirectory)
              ?? throw new InvalidOperationException("Ejecute el comando desde la carpeta backend/ del repositorio.");
var carpetaAudios = Path.Combine(raizApi, "AudiosDemo");
var carpetaGuiones = Path.Combine(carpetaAudios, "guiones");

var apiKey = Environment.GetEnvironmentVariable("Deepgram__ApiKey") ?? LeerDeEnv(Path.Combine(raizApi, ".env"), "Deepgram__ApiKey");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Falta Deepgram__ApiKey (variable de entorno o Qualia.API/.env).");
    return 1;
}

using var http = new HttpClient { BaseAddress = new Uri("https://api.deepgram.com/"), Timeout = TimeSpan.FromSeconds(60) };
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", apiKey);

var opcionesJson = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
var caracteresSintetizados = 0;

foreach (var rutaGuion in Directory.GetFiles(carpetaGuiones, "*.json").Order())
{
    var guion = JsonNode.Parse(await File.ReadAllTextAsync(rutaGuion))!.AsObject();
    var archivo = guion["archivo"]!.GetValue<string>();
    var rutaWav = Path.Combine(carpetaAudios, archivo);
    var rutaGround = Path.ChangeExtension(rutaWav, ".ground.json");

    if (!forzar && File.Exists(rutaWav) && File.Exists(rutaGround))
    {
        Console.WriteLine($"= {archivo} (ya existe)");
        continue;
    }

    var actores = guion["actores"]!.AsObject();
    using var pcm = new MemoryStream();
    var segmentos = new JsonArray();
    EscribirSilencio(pcm, SilencioInicial);

    var orden = 1;
    foreach (var turno in guion["turnos"]!.AsArray())
    {
        var actor = turno!["actor"]!.GetValue<string>();
        var texto = turno["texto"]!.GetValue<string>();
        var voz = actores[actor]!["voz"]!.GetValue<string>();

        var audio = await SintetizarAsync(texto, voz);
        caracteresSintetizados += texto.Length;

        var inicio = Segundos(pcm.Length);
        pcm.Write(audio);
        var fin = Segundos(pcm.Length);
        EscribirSilencio(pcm, PausaEntreTurnos);

        segmentos.Add(new JsonObject
        {
            ["orden"] = orden++,
            ["actor"] = actor,
            ["voz"] = voz,
            ["inicioSeg"] = Math.Round(inicio, 2),
            ["finSeg"] = Math.Round(fin, 2),
            ["texto"] = texto,
        });
    }

    EscribirSilencio(pcm, SilencioFinal - PausaEntreTurnos);
    var duracion = Math.Round(Segundos(pcm.Length), 2);

    Directory.CreateDirectory(Path.GetDirectoryName(rutaWav)!);
    await File.WriteAllBytesAsync(rutaWav, CrearWav(pcm.ToArray()));

    var ground = new JsonObject
    {
        ["archivo"] = archivo,
        ["guion"] = Path.GetRelativePath(carpetaAudios, rutaGuion).Replace('\\', '/'),
        ["descripcion"] = guion["descripcion"]?.GetValue<string>(),
        ["generadoCon"] = "Deepgram Aura-2 TTS",
        ["frecuenciaMuestreo"] = FrecuenciaMuestreo,
        ["duracionSeg"] = duracion,
        ["actores"] = actores.DeepClone(),
        ["segmentos"] = segmentos,
    };
    await File.WriteAllTextAsync(rutaGround, ground.ToJsonString(opcionesJson));

    Console.WriteLine($"+ {archivo}: {duracion:0.00} s, {segmentos.Count} turnos");
}

Console.WriteLine($"Caracteres sintetizados: {caracteresSintetizados}");
return 0;

async Task<byte[]> SintetizarAsync(string texto, string voz)
{
    var url = $"v1/speak?model={voz}&encoding=linear16&sample_rate={FrecuenciaMuestreo}&container=none";
    var cuerpo = new StringContent(JsonSerializer.Serialize(new { text = texto }), Encoding.UTF8, "application/json");

    for (var intento = 1; ; intento++)
    {
        using var respuesta = await http.PostAsync(url, cuerpo);
        if (respuesta.IsSuccessStatusCode)
        {
            return await respuesta.Content.ReadAsByteArrayAsync();
        }

        if ((int)respuesta.StatusCode == 429 && intento < 4)
        {
            await Task.Delay(TimeSpan.FromSeconds(2 * intento));
            continue;
        }

        throw new InvalidOperationException(
            $"Deepgram TTS respondió {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
    }
}

static double Segundos(long bytes) => (double)bytes / (FrecuenciaMuestreo * BytesPorMuestra);

static void EscribirSilencio(Stream destino, double segundos)
{
    var bytes = (int)(segundos * FrecuenciaMuestreo) * BytesPorMuestra;
    if (bytes > 0)
    {
        destino.Write(new byte[bytes]);
    }
}

static byte[] CrearWav(byte[] pcm)
{
    using var salida = new MemoryStream(44 + pcm.Length);
    using var w = new BinaryWriter(salida);
    w.Write("RIFF"u8);
    w.Write(36 + pcm.Length);
    w.Write("WAVE"u8);
    w.Write("fmt "u8);
    w.Write(16);
    w.Write((short)1);                                   // PCM
    w.Write((short)1);                                   // mono
    w.Write(FrecuenciaMuestreo);
    w.Write(FrecuenciaMuestreo * BytesPorMuestra);       // bytes por segundo
    w.Write((short)BytesPorMuestra);                     // alineación
    w.Write((short)(BytesPorMuestra * 8));               // bits por muestra
    w.Write("data"u8);
    w.Write(pcm.Length);
    w.Write(pcm);
    w.Flush();
    return salida.ToArray();
}

static string? BuscarCarpetaApi(string desde)
{
    for (var dir = new DirectoryInfo(desde); dir is not null; dir = dir.Parent)
    {
        foreach (var candidato in new[] { Path.Combine(dir.FullName, "Qualia.API"), Path.Combine(dir.FullName, "backend", "Qualia.API") })
        {
            if (Directory.Exists(Path.Combine(candidato, "AudiosDemo", "guiones")))
            {
                return candidato;
            }
        }
    }

    return null;
}

static string? LeerDeEnv(string ruta, string clave)
{
    if (!File.Exists(ruta))
    {
        return null;
    }

    return File.ReadLines(ruta)
        .Select(l => l.Trim())
        .Where(l => l.StartsWith(clave + "=", StringComparison.Ordinal))
        .Select(l => l[(clave.Length + 1)..].Trim().Trim('"'))
        .LastOrDefault();
}
