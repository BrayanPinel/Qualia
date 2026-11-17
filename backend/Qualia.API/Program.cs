using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Qualia.API.Configuracion;
using Qualia.API.Middleware;
using Qualia.API.Seguridad;
using Qualia.Application;
using Qualia.Application.Common.Seguridad;
using Qualia.Infrastructure;
using Qualia.Infrastructure.Persistence;
using Scalar.AspNetCore;

// Variables de entorno desde backend/Qualia.API/.env (ignorado por Git).
// Se cargan ANTES de crear el builder y sin sobrescribir (NoClobber): una variable que ya
// existe en el entorno real (Docker, CI, pruebas) tiene prioridad sobre el archivo.
ArchivoEnv.Cargar();

var builder = WebApplication.CreateBuilder(args);

// Capas de Clean Architecture
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// Identidad del usuario actual (claims del token)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualHttp>();

// Autenticación. PROVISIONAL hasta integrar el JWT del Rol A (identidad y seguridad).
var modoDemo = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Autenticacion:ModoDemo");
if (modoDemo)
{
    builder.Services.AddAuthentication(AutenticacionDemoHandler.Esquema)
        .AddScheme<AuthenticationSchemeOptions, AutenticacionDemoHandler>(AutenticacionDemoHandler.Esquema, null);
}
else
{
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Enumeraciones como texto ("EnCola", "Borrador"...), como en el contrato.
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Campos desconocidos de entrada se rechazan.
        o.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    });

// Errores de enlace de modelo (JSON mal formado, campos desconocidos, tipos inválidos)
// con el mismo formato ErrorDto que el middleware global, en lugar de ProblemDetails.
builder.Services.Configure<ApiBehaviorOptions>(o =>
    o.InvalidModelStateResponseFactory = RespuestaModeloInvalido.Crear);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await InicializadorBaseDatos.InicializarAsync(app.Services, app.Configuration);
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseMiddleware<ManejadorErroresMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Necesario para las pruebas de integración (WebApplicationFactory<Program>).
public partial class Program;
