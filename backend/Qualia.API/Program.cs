using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication;
using Qualia.API.Middleware;
using Qualia.API.Seguridad;
using Qualia.Application;
using Qualia.Application.Comun.Seguridad;
using Qualia.Infrastructure;
using Qualia.Infrastructure.Persistencia;
using Scalar.AspNetCore;

// Variables de entorno desde backend/Qualia.API/.env .
// NoClobber: si la variable ya existe en el entorno real (Docker, Render, CI), esa tiene prioridad sobre el .env.
Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Capas de Clean Architecture
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// Identidad del usuario actual (claims del token)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualHttp>();

// Autenticación. PROVISIONAL: DE PRUEBA EN LO QUE SE CONECTA CON LA PARTE A
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
        // Enumeraciones como texto ("EnCola", "EvaluadaPorIA"...), como en el contrato.
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    });

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
