using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Qualia.API.Seguridad;

namespace Qualia.API.Configuracion;

/// Declara en el documento OpenAPI cómo se autentica la API, para que Scalar muestre
/// el campo del token y lo envíe solo en los endpoints protegidos.
/// Bearer: token JWT de POST /api/auth/login o /api/plataforma/auth/login.
/// X-Usuario-Demo: solo en Development con Autenticacion:ModoDemo=true (id de un usuario demo).
public static class SeguridadOpenApi
{
    public const string Bearer = "Bearer";
    public const string UsuarioDemo = "UsuarioDemo";

    public static OpenApiOptions AgregarSeguridadQualia(this OpenApiOptions opciones, bool modoDemo)
    {
        opciones.AddDocumentTransformer((documento, _, _) =>
        {
            documento.Components ??= new OpenApiComponents();
            documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            documento.Components.SecuritySchemes[Bearer] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "accessToken devuelto por el login.",
            };

            if (modoDemo)
            {
                documento.Components.SecuritySchemes[UsuarioDemo] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = AutenticacionDemoHandler.Cabecera,
                    Description = "Solo desarrollo: id de un usuario demo (ver Persistence/Semillas/DatosDemo.cs).",
                };
            }

            return Task.CompletedTask;
        });

        // Cada esquema va en un requisito aparte: basta con uno de los dos.
        opciones.AddOperationTransformer((operacion, contexto, _) =>
        {
            var metadatos = contexto.Description.ActionDescriptor.EndpointMetadata;
            if (metadatos.OfType<IAllowAnonymous>().Any() || !metadatos.OfType<IAuthorizeData>().Any())
            {
                return Task.CompletedTask;
            }

            operacion.Security ??= [];
            operacion.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(Bearer, contexto.Document)] = [],
            });

            if (modoDemo)
            {
                operacion.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(UsuarioDemo, contexto.Document)] = [],
                });
            }

            return Task.CompletedTask;
        });

        return opciones;
    }
}
