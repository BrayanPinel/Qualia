using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Qualia.API.Seguridad;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Options;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Autenticacion;
using Qualia.Application.Features.Autenticacion.Dtos;
using Qualia.Application.Features.Empresas.Dtos;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Domain.Enums;
using Qualia.Infrastructure.Persistence;
using Qualia.Infrastructure.Persistence.Semillas;
using Qualia.Infrastructure.Seguridad;
using Qualia.Tests.Integracion.Infraestructura;
using Norte = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Norte;

namespace Qualia.Tests.Integracion;

/// <summary>
/// Login empresarial y de plataforma con JWT contra PostgreSQL real: credenciales, aislamiento por empresa,
/// cuentas inactivas, tokens alterados o expirados y separación entre PlatformAdmin y usuarios de empresa.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class AutenticacionIntegracionTests : IAsyncLifetime
{
    private readonly QualiaApiFactory _api;

    public AutenticacionIntegracionTests(PostgresFixture postgres)
    {
        _api = new QualiaApiFactory(postgres, $"qualia_auth_{Guid.NewGuid():N}");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [FactConDocker]
    public async Task Login_correcto_emite_token_que_abre_los_endpoints_de_la_empresa()
    {
        var respuesta = await LoginAsync(" DEMO-Norte ", " Ana.Admin@Norte.Demo ", DatosDemo.Contrasena);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var sesion = await QualiaApiFactory.LeerAsync<SesionDto>(respuesta);
        Assert.Equal("Bearer", sesion.TokenType);
        Assert.Equal(Norte.Empresa, sesion.EmpresaId);
        Assert.Equal(Norte.Admin, sesion.Usuario.Id);
        Assert.Equal(RolEmpresa.AdminEmpresa, sesion.Usuario.Rol);
        Assert.True(sesion.ExpiraEn > DateTime.UtcNow);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(sesion.AccessToken);
        Assert.Equal(Norte.Empresa.ToString(), token.Claims.Single(c => c.Type == ClaimsQualia.EmpresaId).Value);
        Assert.Equal("AdminEmpresa", token.Claims.Single(c => c.Type == ClaimsQualia.Rol).Value);
        Assert.DoesNotContain(token.Claims, c => c.Value.Contains(DatosDemo.Contrasena));

        var campanas = await Con(sesion.AccessToken).GetAsync("/api/campanas");
        Assert.Equal(HttpStatusCode.OK, campanas.StatusCode);
    }

    [FactConDocker]
    public async Task Empresa_correo_o_contrasena_incorrectos_responden_el_mismo_401()
    {
        HttpResponseMessage[] respuestas =
        [
            await LoginAsync("demo-norte", "ana.admin@norte.demo", "Contrasena-Equivocada"),
            await LoginAsync("demo-norte", "nadie@norte.demo", DatosDemo.Contrasena),
            await LoginAsync("no-existe", "ana.admin@norte.demo", DatosDemo.Contrasena),
            // Usuario real de Sur con el código de Norte: el correo se busca solo dentro de la empresa indicada.
            await LoginAsync("demo-norte", "silvia.supervisora@sur.demo", DatosDemo.Contrasena),
        ];

        foreach (var respuesta in respuestas)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
            var error = await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta);
            Assert.Equal(InicioSesion.CodigoCredencialesInvalidas, error.Codigo);
            Assert.Equal("Credenciales inválidas.", error.Mensaje);
        }
    }

    [FactConDocker]
    public async Task El_mismo_correo_en_dos_empresas_inicia_sesion_en_la_empresa_indicada()
    {
        var planes = await QualiaApiFactory.LeerAsync<List<PlanDto>>(await _api.ClienteAnonimo().GetAsync("/api/planes"));
        var uno = await RegistrarAsync("empresa-uno", "compartido@correo.com", "Contrasena-Empresa-Uno", planes[0].Id);
        var dos = await RegistrarAsync("empresa-dos", "compartido@correo.com", "Contrasena-Empresa-Dos", planes[0].Id);

        var sesionUno = await QualiaApiFactory.LeerAsync<SesionDto>(await LoginAsync("empresa-uno", "compartido@correo.com", "Contrasena-Empresa-Uno"));
        var sesionDos = await QualiaApiFactory.LeerAsync<SesionDto>(await LoginAsync("empresa-dos", "compartido@correo.com", "Contrasena-Empresa-Dos"));
        var cruzado = await LoginAsync("empresa-uno", "compartido@correo.com", "Contrasena-Empresa-Dos");

        Assert.Equal(uno.EmpresaId, sesionUno.EmpresaId);
        Assert.Equal(dos.EmpresaId, sesionDos.EmpresaId);
        Assert.Equal(HttpStatusCode.Unauthorized, cruzado.StatusCode);
    }

    [FactConDocker]
    public async Task Usuario_inactivo_y_empresa_suspendida_responden_403_solo_con_contrasena_correcta()
    {
        await using (var alcance = _api.Services.CreateAsyncScope())
        {
            var db = alcance.ServiceProvider.GetRequiredService<QualiaDbContext>();
            // Preparación directa de datos de dos empresas: se ignora el filtro a propósito.
            var agente = await db.Usuarios.IgnoreQueryFilters().SingleAsync(u => u.Id == Norte.AgenteAndres);
            agente.Activo = false;
            var sur = await db.Empresas.SingleAsync(e => e.Id == DatosDemo.Sur.Empresa);
            sur.Estado = EstadoEmpresa.Suspendida;
            await db.SaveChangesAsync();
        }

        var inactivo = await LoginAsync("demo-norte", "andres.agente@norte.demo", DatosDemo.Contrasena);
        var suspendida = await LoginAsync("demo-sur", "silvia.supervisora@sur.demo", DatosDemo.Contrasena);
        var inactivoMalaClave = await LoginAsync("demo-norte", "andres.agente@norte.demo", "Contrasena-Equivocada");

        Assert.Equal(HttpStatusCode.Forbidden, inactivo.StatusCode);
        Assert.Equal(InicioSesion.CodigoCuentaInactiva, (await QualiaApiFactory.LeerAsync<ErrorDto>(inactivo)).Codigo);
        Assert.Equal(HttpStatusCode.Forbidden, suspendida.StatusCode);
        Assert.Equal(InicioSesion.CodigoEmpresaSuspendida, (await QualiaApiFactory.LeerAsync<ErrorDto>(suspendida)).Codigo);
        Assert.Equal(HttpStatusCode.Unauthorized, inactivoMalaClave.StatusCode);
    }

    [FactConDocker]
    public async Task Sin_token_con_token_alterado_o_de_otra_clave_responde_401_con_ErrorDto()
    {
        var valido = (await QualiaApiFactory.LeerAsync<SesionDto>(await LoginAsync("demo-norte", "ana.admin@norte.demo", DatosDemo.Contrasena))).AccessToken;
        var alterado = valido[..^4] + (valido[^4] == 'A' ? "BBBB" : "AAAA");
        var otraClave = Emitir(new SymmetricSecurityKey(new byte[64]), DateTime.UtcNow.AddMinutes(10));

        var sinToken = await _api.ClienteAnonimo().GetAsync("/api/campanas");
        var conAlterado = await Con(alterado).GetAsync("/api/campanas");
        var conOtraClave = await Con(otraClave).GetAsync("/api/campanas");

        foreach (var (respuesta, codigo) in new[] { (sinToken, "NO_AUTENTICADO"), (conAlterado, "TOKEN_INVALIDO"), (conOtraClave, "TOKEN_INVALIDO") })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
            Assert.Equal(codigo, (await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta)).Codigo);
            Assert.Contains("Bearer", respuesta.Headers.WwwAuthenticate.ToString());
        }
    }

    [FactConDocker]
    public async Task Token_expirado_responde_401_TOKEN_EXPIRADO()
    {
        var clave = _api.Services.GetRequiredService<ClaveJwt>().Clave;
        var expirado = Emitir(clave, DateTime.UtcNow.AddMinutes(-5));

        var respuesta = await Con(expirado).GetAsync("/api/campanas");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("TOKEN_EXPIRADO", (await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta)).Codigo);
    }

    [FactConDocker]
    public async Task Rol_insuficiente_responde_403_con_ErrorDto()
    {
        var agente = (await QualiaApiFactory.LeerAsync<SesionDto>(await LoginAsync("demo-norte", "andres.agente@norte.demo", DatosDemo.Contrasena))).AccessToken;

        // [Authorize(Roles = "PlatformAdmin")] del pipeline y RequerirRol de Application producen el mismo formato.
        var plataforma = await Con(agente).GetAsync("/api/plataforma/empresas");
        var crearCampana = await Con(agente).PostAsJsonAsync("/api/campanas", new { nombre = "No permitida", activa = true });

        foreach (var respuesta in new[] { plataforma, crearCampana })
        {
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("ROL_NO_AUTORIZADO", (await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta)).Codigo);
        }
    }

    [FactConDocker]
    public async Task Login_de_plataforma_da_acceso_comercial_pero_no_al_contenido_de_empresas()
    {
        var respuesta = await _api.ClienteAnonimo().PostAsJsonAsync("/api/plataforma/auth/login",
            new LoginPlataformaDto { Email = " ADMIN@qualia.demo", Password = DatosDemo.Contrasena }, QualiaApiFactory.Json);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var sesion = await QualiaApiFactory.LeerAsync<SesionPlataformaDto>(respuesta);
        Assert.Equal(DatosDemo.Plataforma.Administrador, sesion.AdministradorId);
        Assert.Equal("PlatformAdmin", sesion.Rol);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(sesion.AccessToken);
        Assert.DoesNotContain(token.Claims, c => c.Type == ClaimsQualia.EmpresaId);

        var cliente = Con(sesion.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/plataforma/empresas")).StatusCode);
        foreach (var ruta in new[] { "/api/llamadas", "/api/campanas" })
        {
            var contenido = await cliente.GetAsync(ruta);
            Assert.Equal(HttpStatusCode.Forbidden, contenido.StatusCode);
        }

        var incorrecto = await _api.ClienteAnonimo().PostAsJsonAsync("/api/plataforma/auth/login",
            new LoginPlataformaDto { Email = "admin@qualia.demo", Password = "Contrasena-Equivocada" }, QualiaApiFactory.Json);
        Assert.Equal(HttpStatusCode.Unauthorized, incorrecto.StatusCode);
    }

    [FactConDocker]
    public async Task Un_usuario_de_empresa_no_inicia_sesion_en_la_plataforma()
    {
        var respuesta = await _api.ClienteAnonimo().PostAsJsonAsync("/api/plataforma/auth/login",
            new LoginPlataformaDto { Email = "ana.admin@norte.demo", Password = DatosDemo.Contrasena }, QualiaApiFactory.Json);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [FactConDocker]
    public async Task Un_hash_con_parametros_antiguos_se_actualiza_al_iniciar_sesion()
    {
        var antiguo = new ServicioHashContrasena(Options.Create(new OpcionesHashContrasena { Iteraciones = OpcionesHashContrasena.MinimoIteraciones }))
            .Hashear(DatosDemo.Contrasena);
        await using (var alcance = _api.Services.CreateAsyncScope())
        {
            var db = alcance.ServiceProvider.GetRequiredService<QualiaDbContext>();
            var auditor = await db.Usuarios.IgnoreQueryFilters().SingleAsync(u => u.Id == Norte.Auditor);
            auditor.PasswordHash = antiguo;
            await db.SaveChangesAsync();
        }

        var respuesta = await LoginAsync("demo-norte", "arturo.auditor@norte.demo", DatosDemo.Contrasena);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await using var verificacion = _api.Services.CreateAsyncScope();
        var nuevo = await verificacion.ServiceProvider.GetRequiredService<QualiaDbContext>()
            .Usuarios.IgnoreQueryFilters().Where(u => u.Id == Norte.Auditor).Select(u => u.PasswordHash).SingleAsync();
        var hash = verificacion.ServiceProvider.GetRequiredService<IServicioHashContrasena>();
        Assert.NotEqual(antiguo, nuevo);
        Assert.Equal(ResultadoVerificacionContrasena.Correcta, hash.Verificar(nuevo, DatosDemo.Contrasena));
    }

    [FactConDocker]
    public async Task Login_sin_campos_responde_400()
    {
        var respuesta = await _api.ClienteAnonimo().PostAsJsonAsync("/api/auth/login", new LoginDto(), QualiaApiFactory.Json);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("VALIDACION", (await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta)).Codigo);
    }

    private Task<HttpResponseMessage> LoginAsync(string codigo, string email, string password) =>
        _api.ClienteAnonimo().PostAsJsonAsync("/api/auth/login",
            new LoginDto { CodigoEmpresa = codigo, Email = email, Password = password }, QualiaApiFactory.Json);

    private async Task<EmpresaRegistradaDto> RegistrarAsync(string codigo, string email, string password, Guid planId)
    {
        var respuesta = await _api.ClienteAnonimo().PostAsJsonAsync("/api/empresas", new RegistrarEmpresaDto
        {
            CodigoEmpresa = codigo,
            NombreEmpresa = $"Empresa {codigo}",
            PlanId = planId,
            NombreAdministrador = "Administrador",
            Email = email,
            Password = password,
        }, QualiaApiFactory.Json);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return await QualiaApiFactory.LeerAsync<EmpresaRegistradaDto>(respuesta);
    }

    private HttpClient Con(string token)
    {
        var cliente = _api.ClienteAnonimo();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    /// <summary>Token con los claims de Ana (AdminEmpresa de Norte) firmado con la clave indicada.</summary>
    private string Emitir(SecurityKey clave, DateTime expira)
    {
        var jwt = _api.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimsQualia.UsuarioId, Norte.Admin.ToString()),
                new Claim(ClaimsQualia.EmpresaId, Norte.Empresa.ToString()),
                new Claim(ClaimsQualia.Rol, "AdminEmpresa"),
            ]),
            IssuedAt = expira.AddMinutes(-60),
            NotBefore = expira.AddMinutes(-60),
            Expires = expira,
            SigningCredentials = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256),
        });
    }
}
