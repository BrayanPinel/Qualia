using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Autenticacion.Dtos;
using Qualia.Application.Features.Campanas.Dtos;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Domain.Entidades;
using Qualia.Infrastructure.Persistence;
using Qualia.Infrastructure.Persistence.Semillas;
using Qualia.Tests.Integracion.Infraestructura;
using Norte = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Norte;
using Sur = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Sur;

namespace Qualia.Tests.Integracion;

/// HU-08 de punta a punta con un JWT real de la empresa Norte: cualquier recurso de Sur responde 404
/// en todos los módulos, los listados no lo incluyen y PostgreSQL rechaza las FK entre empresas.
[Collection(ColeccionPostgres.Nombre)]
public sealed class AislamientoIntegracionTests : IAsyncLifetime
{
    private const string ViolacionFk = "23503";
    private readonly QualiaApiFactory _api;

    public AislamientoIntegracionTests(PostgresFixture postgres)
    {
        _api = new QualiaApiFactory(postgres, $"qualia_aislamiento_{Guid.NewGuid():N}");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [FactConDocker]
    public async Task Todo_recurso_de_otra_empresa_responde_404_en_cada_modulo()
    {
        var norte = await ClienteJwtAsync("demo-norte", "ana.admin@norte.demo");
        var llamadaSur = Sur.Llamadas[0];
        var rubrica = new { nombre = "Intento", criterios = new[] { new { nombre = "Criterio uno", descripcion = "Descripción del criterio", peso = 100, esCritico = false, umbralCumplimiento = 70 } } };

        HttpResponseMessage[] respuestas =
        [
            await norte.GetAsync($"/api/llamadas/{llamadaSur}"),
            await norte.GetAsync($"/api/llamadas/{llamadaSur}/transcripcion"),
            await norte.GetAsync($"/api/llamadas/{llamadaSur}/audio"),
            await norte.GetAsync($"/api/llamadas/{llamadaSur}/evaluacion"),
            await norte.PutAsJsonAsync($"/api/campanas/{Sur.Campana}", new { nombre = "Secuestrada", activa = false }),
            await norte.GetAsync($"/api/campanas/{Sur.Campana}/usuarios"),
            await norte.PutAsync($"/api/campanas/{Sur.Campana}/usuarios/{Norte.AgenteAndres}", null),
            await norte.PutAsync($"/api/campanas/{Norte.Campana}/usuarios/{Sur.Agente}", null),
            await norte.GetAsync($"/api/campanas/{Sur.Campana}/rubricas"),
            await norte.PutAsJsonAsync($"/api/rubricas/{Sur.Rubrica}", rubrica),
            await norte.PostAsync($"/api/rubricas/{Sur.Rubrica}/publicar", null),
            await norte.PutAsJsonAsync($"/api/usuarios/{Sur.Supervisor}", new { nombre = "Secuestrada", activo = false }),
        ];

        foreach (var r in respuestas)
        {
            Assert.True(r.StatusCode == HttpStatusCode.NotFound,
                $"{r.RequestMessage!.Method} {r.RequestMessage.RequestUri!.AbsolutePath} => {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        }
    }

    [FactConDocker]
    public async Task Los_listados_solo_muestran_datos_de_la_empresa_de_la_sesion()
    {
        var norte = await ClienteJwtAsync("demo-norte", "ana.admin@norte.demo");
        var sur = await ClienteJwtAsync("demo-sur", "silvia.supervisora@sur.demo");

        var llamadasNorte = await QualiaApiFactory.LeerAsync<PaginaDto<LlamadaDto>>(await norte.GetAsync("/api/llamadas?tamanoPagina=100"));
        var campanasNorte = await QualiaApiFactory.LeerAsync<PaginaDto<CampanaDto>>(await norte.GetAsync("/api/campanas?tamanoPagina=100"));
        var usuariosNorte = await QualiaApiFactory.LeerAsync<PaginaDto<UsuarioDto>>(await norte.GetAsync("/api/usuarios?tamanoPagina=100"));
        var campanasSur = await QualiaApiFactory.LeerAsync<PaginaDto<CampanaDto>>(await sur.GetAsync("/api/campanas?tamanoPagina=100"));

        Assert.NotEmpty(llamadasNorte.Items);
        Assert.DoesNotContain(llamadasNorte.Items, l => Sur.Llamadas.Contains(l.Id));
        Assert.All(campanasNorte.Items, c => Assert.Equal(Norte.Empresa, c.EmpresaId));
        Assert.All(usuariosNorte.Items, u => Assert.Equal(Norte.Empresa, u.EmpresaId));
        Assert.All(campanasSur.Items, c => Assert.Equal(Sur.Empresa, c.EmpresaId));
    }

    [FactConDocker]
    public async Task PostgreSQL_rechaza_relaciones_entre_registros_de_empresas_distintas()
    {
        await AssertViolacionFkAsync(db => db.UsuariosCampana.Add(new UsuarioCampana
        {
            EmpresaId = Norte.Empresa, CampanaId = Norte.Campana, UsuarioId = Sur.Agente,
        }));

        await AssertViolacionFkAsync(db => db.Auditorias.Add(new Auditoria
        {
            EmpresaId = Norte.Empresa, UsuarioId = Sur.Supervisor, Accion = "Prueba", Entidad = "Usuario", Fecha = DateTime.UtcNow,
        }));
    }

    private async Task AssertViolacionFkAsync(Action<QualiaDbContext> agregar)
    {
        await using var alcance = _api.Services.CreateAsyncScope();
        var db = alcance.ServiceProvider.GetRequiredService<QualiaDbContext>();
        agregar(db);

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(ViolacionFk, Assert.IsType<PostgresException>(error.InnerException).SqlState);
    }

    private async Task<HttpClient> ClienteJwtAsync(string codigo, string email)
    {
        var respuesta = await _api.ClienteAnonimo().PostAsJsonAsync("/api/auth/login",
            new LoginDto { CodigoEmpresa = codigo, Email = email, Password = DatosDemo.Contrasena }, QualiaApiFactory.Json);
        var sesion = await QualiaApiFactory.LeerAsync<SesionDto>(respuesta);
        var cliente = _api.ClienteAnonimo();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.AccessToken);
        return cliente;
    }
}
