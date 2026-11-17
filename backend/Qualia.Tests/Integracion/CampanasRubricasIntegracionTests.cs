using System.Net;
using System.Net.Http.Json;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Campanas.Dtos;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Domain.Enums;
using Qualia.Tests.Integracion.Infraestructura;
using Norte = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Norte;
using Sur = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Sur;

namespace Qualia.Tests.Integracion;

/// <summary>
/// Rol B contra PostgreSQL real: campañas, asignaciones y rúbricas versionadas, con permisos por rol
/// y aislamiento entre las empresas Norte y Sur.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class CampanasRubricasIntegracionTests : IAsyncLifetime
{
    private readonly QualiaApiFactory _api;

    public CampanasRubricasIntegracionTests(PostgresFixture postgres)
    {
        _api = new QualiaApiFactory(postgres, $"qualia_campanas_{Guid.NewGuid():N}");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private static object Rubrica(string nombre, params (string Nombre, decimal Peso, bool Critico)[] criterios) => new
    {
        nombre,
        criterios = criterios.Select(c => new
        {
            nombre = c.Nombre,
            descripcion = $"Descripción de {c.Nombre}",
            peso = c.Peso,
            esCritico = c.Critico,
            umbralCumplimiento = 70m,
        }),
    };

    private static async Task<string> CodigoAsync(HttpResponseMessage r) => (await QualiaApiFactory.LeerAsync<ErrorDto>(r)).Codigo;

    // ------------------------------------------------------------------ campañas

    [FactConDocker]
    public async Task AdminEmpresa_crea_y_actualiza_campanas_y_el_listado_respeta_el_alcance_por_rol()
    {
        var admin = _api.ClienteComo(Norte.Admin);

        var creada = await admin.PostAsJsonAsync("/api/campanas", new { nombre = "  Ventas Cruzadas  ", activa = true });
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        var campana = await QualiaApiFactory.LeerAsync<CampanaDto>(creada);
        Assert.Equal("Ventas Cruzadas", campana.Nombre);
        Assert.Equal(Norte.Empresa, campana.EmpresaId);
        Assert.Null(campana.RubricaActivaId);

        Assert.Equal(HttpStatusCode.NoContent,
            (await admin.PutAsJsonAsync($"/api/campanas/{campana.Id}", new { nombre = "Ventas Cruzadas 2027", activa = false })).StatusCode);

        // AdminEmpresa y Auditor ven todas las campañas de su empresa (y ninguna de Sur).
        var deAdmin = await QualiaApiFactory.LeerAsync<PaginaDto<CampanaDto>>(await admin.GetAsync("/api/campanas"));
        Assert.Equal(2, deAdmin.Total);
        Assert.Contains(deAdmin.Items, c => c.Id == campana.Id && c.Nombre == "Ventas Cruzadas 2027" && !c.Activa);
        Assert.Contains(deAdmin.Items, c => c.Id == Norte.Campana && c.RubricaActivaId == Norte.Rubrica);
        Assert.DoesNotContain(deAdmin.Items, c => c.Id == Sur.Campana);

        var deAuditor = await QualiaApiFactory.LeerAsync<PaginaDto<CampanaDto>>(await _api.ClienteComo(Norte.Auditor).GetAsync("/api/campanas"));
        Assert.Equal(2, deAuditor.Total);

        // Supervisor y Agente solo ven las campañas asignadas.
        var deSupervisora = await QualiaApiFactory.LeerAsync<PaginaDto<CampanaDto>>(await _api.ClienteComo(Norte.Supervisora).GetAsync("/api/campanas"));
        Assert.Equal([Norte.Campana], deSupervisora.Items.Select(c => c.Id));

        var deSupervisorSinCampana = await QualiaApiFactory.LeerAsync<PaginaDto<CampanaDto>>(
            await _api.ClienteComo(Norte.SupervisorSinCampana).GetAsync("/api/campanas"));
        Assert.Equal(0, deSupervisorSinCampana.Total);

        var deAgente = await QualiaApiFactory.LeerAsync<PaginaDto<CampanaDto>>(await _api.ClienteComo(Norte.AgenteAndres).GetAsync("/api/campanas"));
        Assert.Equal([Norte.Campana], deAgente.Items.Select(c => c.Id));
    }

    [FactConDocker]
    public async Task Solo_AdminEmpresa_gestiona_campanas()
    {
        var supervisora = _api.ClienteComo(Norte.Supervisora);

        var crear = await supervisora.PostAsJsonAsync("/api/campanas", new { nombre = "No permitida", activa = true });
        Assert.Equal(HttpStatusCode.Forbidden, crear.StatusCode);
        Assert.Equal("ROL_NO_AUTORIZADO", await CodigoAsync(crear));

        Assert.Equal(HttpStatusCode.Forbidden, (await supervisora.PutAsync($"/api/campanas/{Norte.Campana}/usuarios/{Norte.SupervisorSinCampana}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _api.ClienteComo(Norte.AgenteAndres).GetAsync($"/api/campanas/{Norte.Campana}/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.ClienteAnonimo().GetAsync("/api/campanas")).StatusCode);
    }

    [FactConDocker]
    public async Task Errores_de_validacion_y_campos_desconocidos_responden_ErrorDto_uniforme()
    {
        var admin = _api.ClienteComo(Norte.Admin);

        var corta = await admin.PostAsJsonAsync("/api/campanas", new { nombre = "AB" });
        Assert.Equal(HttpStatusCode.BadRequest, corta.StatusCode);
        var error = await QualiaApiFactory.LeerAsync<ErrorDto>(corta);
        Assert.Equal("VALIDACION", error.Codigo);
        Assert.Contains("Nombre", error.Errores!.Keys);
        Assert.Contains("Activa", error.Errores!.Keys);

        var desconocido = await admin.PostAsJsonAsync("/api/campanas", new { nombre = "Campaña válida", activa = true, empresaId = Sur.Empresa });
        Assert.Equal(HttpStatusCode.BadRequest, desconocido.StatusCode);
        var errorDesconocido = await QualiaApiFactory.LeerAsync<ErrorDto>(desconocido);
        Assert.Equal("VALIDACION", errorDesconocido.Codigo);
        Assert.Equal(["Campo no permitido."], errorDesconocido.Errores!["empresaId"]);
        Assert.DoesNotContain("Qualia.", await desconocido.Content.ReadAsStringAsync()); // Sin nombres de tipos internos.

        var sinCuerpo = await admin.PostAsync("/api/campanas", JsonContent.Create<object?>(null));
        Assert.Equal(HttpStatusCode.BadRequest, sinCuerpo.StatusCode);
        Assert.Equal("VALIDACION", await CodigoAsync(sinCuerpo));

        var paginacion = await admin.GetAsync("/api/campanas?tamanoPagina=500");
        Assert.Equal(HttpStatusCode.BadRequest, paginacion.StatusCode);
        Assert.Equal("VALIDACION", await CodigoAsync(paginacion));
    }

    [FactConDocker]
    public async Task Asignaciones_validan_rol_y_empresa_son_idempotentes_y_se_revocan()
    {
        var admin = _api.ClienteComo(Norte.Admin);
        var ruta = $"/api/campanas/{Norte.Campana}/usuarios";

        // Usuario de otra empresa: 404 (no se revela que exista).
        var ajeno = await admin.PutAsync($"{ruta}/{Sur.Agente}", null);
        Assert.Equal(HttpStatusCode.NotFound, ajeno.StatusCode);
        Assert.Equal("USUARIO_NO_ENCONTRADO", await CodigoAsync(ajeno));

        // Solo Agente o Supervisor.
        var auditor = await admin.PutAsync($"{ruta}/{Norte.Auditor}", null);
        Assert.Equal(HttpStatusCode.Conflict, auditor.StatusCode);
        Assert.Equal("ROL_NO_ASIGNABLE", await CodigoAsync(auditor));

        // Idempotente: dos asignaciones dejan una sola fila.
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsync($"{ruta}/{Norte.SupervisorSinCampana}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsync($"{ruta}/{Norte.SupervisorSinCampana}", null)).StatusCode);

        var asignados = await QualiaApiFactory.LeerAsync<PaginaDto<UsuarioDto>>(await admin.GetAsync(ruta));
        Assert.Equal(4, asignados.Total);
        Assert.Single(asignados.Items, u => u.Id == Norte.SupervisorSinCampana);
        Assert.All(asignados.Items, u => Assert.Contains(u.Rol, new[] { RolEmpresa.Agente, RolEmpresa.Supervisor }));

        // El supervisor recién asignado ya ve la campaña y sus rúbricas.
        var samuel = _api.ClienteComo(Norte.SupervisorSinCampana);
        Assert.Equal(HttpStatusCode.OK, (await samuel.GetAsync($"/api/campanas/{Norte.Campana}/rubricas")).StatusCode);

        // Revocar (también idempotente).
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{ruta}/{Norte.SupervisorSinCampana}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{ruta}/{Norte.SupervisorSinCampana}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await samuel.GetAsync($"/api/campanas/{Norte.Campana}/rubricas")).StatusCode);
    }

    [FactConDocker]
    public async Task Una_empresa_no_accede_a_campanas_ni_rubricas_de_otra()
    {
        var adminNorte = _api.ClienteComo(Norte.Admin);

        var actualizar = await adminNorte.PutAsJsonAsync($"/api/campanas/{Sur.Campana}", new { nombre = "Intrusión", activa = true });
        Assert.Equal(HttpStatusCode.NotFound, actualizar.StatusCode);
        Assert.Equal("CAMPANA_NO_ENCONTRADA", await CodigoAsync(actualizar));

        Assert.Equal(HttpStatusCode.NotFound, (await adminNorte.GetAsync($"/api/campanas/{Sur.Campana}/rubricas")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminNorte.GetAsync($"/api/campanas/{Sur.Campana}/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminNorte.PutAsync($"/api/campanas/{Sur.Campana}/usuarios/{Sur.Agente}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await adminNorte.PostAsJsonAsync($"/api/campanas/{Sur.Campana}/rubricas", Rubrica("Intrusa", ("Saludo", 100, false)))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminNorte.PostAsync($"/api/rubricas/{Sur.Rubrica}/publicar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await adminNorte.PutAsJsonAsync($"/api/rubricas/{Sur.Rubrica}", Rubrica("Intrusa", ("Saludo", 100, false)))).StatusCode);

        // El supervisor de Sur tampoco ve las rúbricas de Norte.
        Assert.Equal(HttpStatusCode.NotFound, (await _api.ClienteComo(Sur.Supervisor).GetAsync($"/api/campanas/{Norte.Campana}/rubricas")).StatusCode);
    }

    // ------------------------------------------------------------------ rúbricas

    [FactConDocker]
    public async Task Cada_rubrica_nueva_es_una_version_en_borrador_y_se_valida_al_publicar()
    {
        var admin = _api.ClienteComo(Norte.Admin);

        // Norte ya tiene v1 (Activa) y v2 (Borrador): la nueva es v3.
        var creada = await admin.PostAsJsonAsync($"/api/campanas/{Norte.Campana}/rubricas",
            Rubrica("Calidad 2027", ("Saludo", 40, false), ("Resolución", 50, true)));
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        var rubrica = await QualiaApiFactory.LeerAsync<RubricaDto>(creada);
        Assert.Equal(3, rubrica.Version);
        Assert.Equal(EstadoRubrica.Borrador, rubrica.Estado);
        Assert.Equal(2, rubrica.Criterios.Count);

        // Los pesos suman 90: no se publica.
        var publicar = await admin.PostAsync($"/api/rubricas/{rubrica.Id}/publicar", null);
        Assert.Equal(HttpStatusCode.BadRequest, publicar.StatusCode);
        var error = await QualiaApiFactory.LeerAsync<ErrorDto>(publicar);
        Assert.Equal("RUBRICA_INVALIDA", error.Codigo);
        Assert.Contains(error.Errores!["criterios"], m => m.Contains("100"));

        // Nombres duplicados: se rechazan al guardar.
        var duplicada = await admin.PutAsJsonAsync($"/api/rubricas/{rubrica.Id}",
            Rubrica("Calidad 2027", ("Saludo", 50, false), ("SALUDO", 50, false)));
        Assert.Equal(HttpStatusCode.BadRequest, duplicada.StatusCode);
        Assert.Equal("VALIDACION", await CodigoAsync(duplicada));

        // El borrador se corrige (reemplaza los criterios) y ya puede publicarse.
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/rubricas/{rubrica.Id}",
            Rubrica("Calidad 2027", ("Saludo", 40, false), ("Resolución", 45, true), ("Cierre", 15, false)))).StatusCode);

        var versiones = await QualiaApiFactory.LeerAsync<List<RubricaDto>>(await admin.GetAsync($"/api/campanas/{Norte.Campana}/rubricas"));
        Assert.Equal([3, 2, 1], versiones.Select(v => v.Version));
        Assert.Equal(3, versiones[0].Criterios.Count);
        Assert.Equal(100m, versiones[0].Criterios.Sum(c => c.Peso));
    }

    [FactConDocker]
    public async Task Publicar_retira_la_version_activa_es_inmutable_y_las_llamadas_conservan_su_version()
    {
        var admin = _api.ClienteComo(Norte.Admin);

        var publicada = await admin.PostAsync($"/api/rubricas/{Norte.RubricaBorrador}/publicar", null);
        Assert.Equal(HttpStatusCode.OK, publicada.StatusCode);
        Assert.Equal(EstadoRubrica.Activa, (await QualiaApiFactory.LeerAsync<RubricaDto>(publicada)).Estado);

        var versiones = await QualiaApiFactory.LeerAsync<List<RubricaDto>>(await admin.GetAsync($"/api/campanas/{Norte.Campana}/rubricas"));
        Assert.Equal(EstadoRubrica.Activa, versiones.Single(v => v.Id == Norte.RubricaBorrador).Estado);
        Assert.Equal(EstadoRubrica.Retirada, versiones.Single(v => v.Id == Norte.Rubrica).Estado);
        Assert.Single(versiones, v => v.Estado == EstadoRubrica.Activa);

        var campanas = await QualiaApiFactory.LeerAsync<PaginaDto<CampanaDto>>(await admin.GetAsync("/api/campanas"));
        Assert.Equal(Norte.RubricaBorrador, campanas.Items.Single(c => c.Id == Norte.Campana).RubricaActivaId);

        // Inmutable: ni la nueva activa ni la retirada se editan o vuelven a publicar.
        var editar = await admin.PutAsJsonAsync($"/api/rubricas/{Norte.RubricaBorrador}", Rubrica("Cambio", ("Saludo", 100, false)));
        Assert.Equal(HttpStatusCode.Conflict, editar.StatusCode);
        Assert.Equal("RUBRICA_PUBLICADA", await CodigoAsync(editar));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/rubricas/{Norte.Rubrica}/publicar", null)).StatusCode);

        // RN-07: la llamada histórica sigue en la versión 1.
        var llamada = await QualiaApiFactory.LeerAsync<LlamadaDto>(await admin.GetAsync($"/api/llamadas/{Norte.Llamadas[0]}"));
        Assert.Equal(Norte.Rubrica, llamada.RubricaId);
        Assert.Equal(1, llamada.VersionRubrica);
    }

    [FactConDocker]
    public async Task Rubricas_de_una_campana_solo_para_AdminEmpresa_o_Supervisor_asignado()
    {
        Assert.Equal(HttpStatusCode.OK, (await _api.ClienteComo(Norte.Supervisora).GetAsync($"/api/campanas/{Norte.Campana}/rubricas")).StatusCode);

        var noAsignado = await _api.ClienteComo(Norte.SupervisorSinCampana).GetAsync($"/api/campanas/{Norte.Campana}/rubricas");
        Assert.Equal(HttpStatusCode.Forbidden, noAsignado.StatusCode);
        Assert.Equal("CAMPANA_NO_ASIGNADA", await CodigoAsync(noAsignado));

        Assert.Equal(HttpStatusCode.Forbidden, (await _api.ClienteComo(Norte.AgenteAndres).GetAsync($"/api/campanas/{Norte.Campana}/rubricas")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _api.ClienteComo(Norte.Auditor).GetAsync($"/api/campanas/{Norte.Campana}/rubricas")).StatusCode);

        // El supervisor puede consultar pero no publicar.
        Assert.Equal(HttpStatusCode.Forbidden, (await _api.ClienteComo(Norte.Supervisora).PostAsync($"/api/rubricas/{Norte.RubricaBorrador}/publicar", null)).StatusCode);
    }
}
