using DotNetEnv;
using Qualia.Application;
using Qualia.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Carga .env (ignorado por Git): primero el del proyecto API y, si no existe,
// el que se encuentre en el directorio actual o en sus padres.
var rutaEnvApi = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(rutaEnvApi))
{
    Env.Load(rutaEnvApi);
}
else
{
    Env.TraversePath().Load();
}

// DotNetEnv escribe variables de proceso; como el builder ya leyó el entorno
// al construirse, se vuelve a registrar el proveedor para que la configuración
// vea los valores recién cargados.
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);


builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Necesario para que WebApplicationFactory<Program> pueda iniciar la API en pruebas.
public partial class Program { }
