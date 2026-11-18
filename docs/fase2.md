# Proyecto Final: Qualia
## Fase 2: Construcción del Backend Transaccional (.NET + PostgreSQL) (25%)

**Plataforma SaaS multiempresa de evaluación de calidad de conversaciones con inteligencia artificial**

**Asignatura:** IS-305 Programación Web
**Sección:** 1500  
**Integrantes:** Brayan Pinel, Jonathan Ordoñez, Javier Flores, Marcel Euceda  
**Docente:** CONSTANTINO SORTO REYES

> **Alcance de esta entrega:** backend transaccional en .NET 10 con PostgreSQL: Clean Architecture con Vertical Slicing, persistencia Code First con aislamiento por empresa mediante Global Query Filters, seguridad con JWT, validación con FluentValidation y un formato de error único, motor de evaluación con IA y la batería de pruebas automatizadas.

---

## 1. Visión Global de la Fase 2

### 1.1 Resumen de la entrega

**Qualia: la IA califica, el humano decide cuando hace falta.**

La fase 2 convierte el análisis y los contratos de la fase 1 en una API funcional. El backend implementa los **37 endpoints** definidos en la fase 1, además del endpoint técnico de audio temporal. Persiste las **24 tablas** del modelo relacional en PostgreSQL mediante una migración oficial y aísla los datos de cada empresa de forma automática.

El sistema recibe llamadas precargadas, las encola automáticamente, las transcribe, enmascara datos sensibles, las evalúa con IA contra una rúbrica versionada y presenta evidencia verificable por criterio. Si la IA no detecta fallas en su propia evaluación, el resultado es la calificación oficial. Si la IA marca una falla, la evaluación queda pendiente hasta que el supervisor la confirme o la ajuste. Los scorecards y reportes se calculan a partir de las evaluaciones oficiales.

### 1.2 Objetivo general

Construir un backend transaccional, multiempresa y protegido que cumpla los contratos de la fase 1, garantice el aislamiento de datos entre empresas sin depender de filtros manuales y valide cada entrada antes de llegar a la lógica de negocio.

### 1.3 Objetivos específicos

1. Separar el sistema en las capas Domain, Application, Infrastructure y API, con dependencias que apunten solo hacia el dominio.
2. Organizar la lógica de negocio por funcionalidad (Vertical Slicing) en lugar de por tipo técnico.
3. Persistir el modelo con Entity Framework Core Code First y una migración oficial reproducible.
4. Aplicar un Global Query Filter por `EmpresaId` a todas las entidades empresariales.
5. Autenticar con JWT y transportar la empresa del usuario en el claim `empresa_id`.
6. Almacenar contraseñas únicamente como hash con PBKDF2.
7. Validar entradas con FluentValidation y responder todos los errores con un único `ErrorDto` desde un middleware global.
8. Verificar el comportamiento con pruebas unitarias y de integración contra PostgreSQL real.

### 1.4 Tecnologías utilizadas

| Componente | Tecnología |
|---|---|
| Plataforma | .NET 10 (SDK fijado en `backend/global.json`) y ASP.NET Core |
| Persistencia | Entity Framework Core 10, Npgsql y `EFCore.NamingConventions` (`snake_case`) |
| Base de datos | PostgreSQL 16 en Docker (`infra/docker-compose.yml`), esquema `qualia` |
| Seguridad | `Microsoft.AspNetCore.Authentication.JwtBearer` y `PasswordHasher` de `Microsoft.Extensions.Identity.Core` |
| Validación | FluentValidation con registro automático de validadores |
| Documentación de la API | OpenAPI y Scalar (`/scalar/v1` en Development) |
| IA | Deepgram (transcripción) y Groq (evaluación), con modo simulado sin red |
| Pruebas | xUnit, `Microsoft.AspNetCore.Mvc.Testing` y Testcontainers para PostgreSQL |
| Configuración | `DotNetEnv`: secretos solo en `backend/Qualia.API/.env`, ignorado por Git |

---

## 2. Alcance Implementado

### 2.1 Funcionalidades entregadas

1. Registro de empresa con usuario AdminEmpresa y suscripción inicial en una sola transacción.
2. Inicio de sesión de usuarios de empresa y del administrador de plataforma con JWT.
3. Gestión de usuarios con límite de supervisores según el plan y protección del último AdminEmpresa activo.
4. Catálogo de planes, suscripción, pago simulado, consumo mensual y administración comercial de plataforma.
5. Campañas, asignación de agentes y supervisores, y rúbricas versionadas con publicación inmutable.
6. Evaluación automática de llamadas: cola, transcripción, enmascarado, evaluación con IA y validación de evidencia.
7. Reintento de una llamada perdida reutilizando su trabajo y su consumo, sin doble cobro.
8. Revisión humana: confirmación y ajuste con control de concurrencia, y bandeja de evaluaciones con falla.
9. Disputas del agente y su resolución por el supervisor.
10. Scorecards por agente, scorecards del equipo y reporte agregado por campaña.
11. Auditoría de las operaciones relevantes en la tabla `auditoria`.
12. Acceso temporal y firmado (HMAC) al audio de las llamadas.

### 2.2 Fuera del alcance de esta fase

- Frontend web en React (fase 3).
- Dockerfile de la API y `docker-compose.yml` del sistema completo (fase 4).
- Despliegue en la nube e integración continua (fase 5).
- Carga de audios por el usuario: el MVP trabaja con llamadas ficticias precargadas.

### 2.3 Actores y permisos implementados

| Rol | Permisos implementados | Restricción de alcance |
|---|---|---|
| PlatformAdmin | Consultar empresas y cambiar su suscripción | Sin acceso al contenido de llamadas |
| AdminEmpresa | Usuarios, campañas, rúbricas, suscripción, consumo, llamadas, scorecards y reportes | Solo su empresa |
| Supervisor | Bandeja de revisión, evidencia, confirmar o ajustar, resolver disputas, scorecards del equipo | Solo campañas asignadas |
| Agente | Sus llamadas, evaluaciones y scorecard; abrir disputas | Solo información propia |
| Auditor | Metadatos de campañas y reporte agregado | Solo su empresa; sin audio ni scorecards individuales |

Cada política se valida en el caso de uso: rol, empresa, asignación a campaña, propietario y función del plan, según corresponda.

### 2.4 Planes y funciones aplicadas

| Regla | Implementación |
|---|---|
| Suscripción operativa para evaluar | `SUSCRIPCION_NO_ACTIVA` (403) al reservar minutos |
| Cuota mensual de minutos | Reserva atómica bajo bloqueo del periodo; `CUOTA_AGOTADA` (409) |
| Límite de supervisores y rúbricas activas | `LIMITE_PLAN_SUPERVISORES` y `LIMITE_PLAN_RUBRICAS` |
| Funciones por plan | `IConsultaPlanService.TieneFuncionAsync` con los códigos `SCORECARD_BASICO`, `TENDENCIA`, `COMPARACION_CAMPANAS` y `DISPUTAS` |
| Cambio de plan | Prohibido en un mes con consumo; `CAMBIO_PLAN_PERIODO_EN_USO` (409) |

### 2.5 Ajustes de reglas respecto a la fase 1

Durante la implementación, el equipo actualizó tres reglas. `fase1.md` ya refleja los cambios.

| Regla | Fase 1 | Fase 2 |
|---|---|---|
| Inicio de la evaluación | El supervisor inicia cada evaluación | El worker encola automáticamente las llamadas con rúbrica publicada; el supervisor puede adelantar una llamada |
| Calificación oficial (RN-11) | Solo confirmada o ajustada por el supervisor | También la evaluación de la IA sin fallas detectadas |
| Llamada perdida (RN-13) | Una llamada Fallida no se recupera | El supervisor puede volver a evaluarla; el trabajo y el consumo se reutilizan |

La IA marca una falla en tres casos: evidencia inválida descartada, una falta sin el fragmento que la pruebe, o una nota a menos de 5 puntos del umbral de cumplimiento. El margen se configura con `Procesamiento:MargenRevisionUmbral`.

---

## 3. Arquitectura del Backend

### 3.1 Clean Architecture

| Capa | Proyecto | Responsabilidad | Depende de |
|---|---|---|---|
| Dominio | `Qualia.Domain` | Entidades, enumeraciones, reglas de transición y cálculo de puntajes | Ninguna capa |
| Aplicación | `Qualia.Application` | Casos de uso, DTOs, validadores, contratos entre módulos e interfaces | Domain |
| Infraestructura | `Qualia.Infrastructure` | EF Core, configuraciones, migraciones, repositorios, IA, audio, hash y auditoría | Application |
| Presentación | `Qualia.API` | Controladores, autenticación, middleware y composición de dependencias | Application e Infrastructure |

`Qualia.Domain` no referencia ningún paquete. La API solo compone y expone: los controladores no contienen lógica de negocio y delegan en un caso de uso.

Application depende de la abstracción de EF Core porque `IQualiaDbContext` expone `DbSet`. Es una decisión deliberada para escribir consultas con LINQ sin repositorios genéricos. La implementación concreta (`QualiaDbContext`, Npgsql y migraciones) vive solo en Infrastructure.

### 3.2 Vertical Slicing

La capa de aplicación se organiza por funcionalidad. Cada carpeta contiene sus casos de uso, DTOs y validadores.

| Funcionalidad (`Features/`) | Épica | Contenido principal |
|---|---|---|
| `Empresas`, `Autenticacion`, `Usuarios` | EP-01 | Registro, inicio de sesión y gestión de usuarios |
| `Suscripciones` | EP-02 | Planes, suscripción, pago simulado, consumo y plataforma |
| `Campanas` | EP-03 | Campañas, asignaciones y rúbricas versionadas |
| `Llamadas`, `Procesamiento` | EP-04 | Consultas, inicio de evaluación, despachador, worker y construcción de la evaluación |
| `Revision`, `Disputas` | EP-05 | Confirmación, ajuste, revisión vigente y disputas |
| `Desempeno` | EP-06 | Scorecards y reporte de campaña |

### 3.3 Estructura de carpetas

```
backend/
├── Qualia.Domain/          Common/, Enums/, Entidades/, Suscripciones/, Servicios/
├── Qualia.Application/     Common/ (Interfaces, Errores, Resultados, Models, Seguridad), Features/
├── Qualia.Infrastructure/  Persistence/ (Configurations, Migrations, Repositories, Semillas),
│                           IA/, Procesamiento/, Audio/, Seguridad/
├── Qualia.API/             Controllers/, Middleware/, Seguridad/, Configuracion/, Qualia.API.http
└── Qualia.Tests/           Unitarias/, Integracion/, Application/, Domain/, API/, Infraestructura/
```

Las carpetas técnicas compartidas usan nombres en inglés (`Common`, `Persistence`) y las de negocio en español (`Features/Llamadas`, `Entidades`).

### 3.4 Contratos entre módulos

Los módulos se comunican mediante interfaces de `Application/Common/Interfaces`. Cada contrato se ejecuta dentro de la transacción de quien lo invoca, sobre el mismo `IQualiaDbContext` del scope.

| Contrato | Expone → usa | Propósito |
|---|---|---|
| `IConsumoEvaluacion` | Suscripciones → Procesamiento | Reservar, registrar uso técnico, facturar y liberar minutos |
| `IConsultaPlanService` | Suscripciones → Campañas, Usuarios, Disputas, Desempeño | Funciones y límites del plan vigente |
| `ICrearSuscripcionInicial` | Suscripciones → Empresas | Suscripción de prueba al registrar la empresa |
| `IConsultaRevisionVigente` | Revisión → Llamadas, Desempeño | Puntaje humano vigente por criterio |
| `IRevisionEvaluacion` | Revisión → Disputas | Resolver disputas con o sin cambio de notas |
| `IRegistroAuditoria` | Identidad → todos | Registrar acciones auditables en la misma transacción |

---

## 4. Historias de Usuario Implementadas

| HU | Historia | Estado | Endpoints principales |
|---|---|---|---|
| HU-01 | Registro de empresa | ✅ | `POST /api/empresas` |
| HU-08 | Aislamiento entre empresas | ✅ | Transversal: Global Query Filter y 404 entre empresas |
| HU-11 | Inicio de sesión | ✅ | `POST /api/auth/login`, `POST /api/plataforma/auth/login` |
| HU-12 | Usuarios y asignaciones | ✅ | `/api/usuarios`, `/api/campanas/{id}/usuarios` |
| HU-06 | Control del plan | ✅ | `/api/planes`, `/api/suscripcion`, `/api/consumo` |
| HU-07 | Administración de plataforma | ✅ | `/api/plataforma/empresas` |
| HU-02 | Campañas y rúbricas versionadas | ✅ | `/api/campanas`, `/api/rubricas` |
| HU-13 | Procesamiento asistido por IA | ✅ | Worker automático y `POST /api/llamadas/{id}/evaluacion` |
| HU-03 | Consulta de evaluación y evidencia | ✅ | `/api/llamadas/{id}/evaluacion`, `/transcripcion`, `/audio` |
| HU-04 | Confirmación o ajuste | ✅ | `/api/evaluaciones/{id}/confirmar`, `/api/evaluaciones/{id}/ajuste` |
| HU-05 | Consulta propia y disputa | ✅ | `/api/evaluaciones/{id}/disputas`, `/api/disputas` |
| HU-09 | Scorecard del agente | ✅ | `GET /api/agentes/me/scorecard` |
| HU-10 | Desempeño del equipo | ✅ | `/api/agentes/{id}/scorecard`, `/api/campanas/{id}/scorecards`, `/api/reportes/campanas/{id}` |

---

## 5. Gobierno Ágil y Flujo de Trabajo

### 5.1 Reparto por roles

| Rol | Alcance |
|---|---|
| A — Identidad y seguridad | Empresa, Usuario, Administrador, Auditoría y Disputa; registro, JWT, usuarios y disputas |
| B — Campañas, rúbricas y revisión | Campaña, asignaciones, rúbricas, revisión humana y evidencias de revisión |
| C — Planes, suscripciones, consumo y reportes | Plan, suscripción, periodos, consumo, plataforma, scorecards y reportes |
| D — Llamadas y procesamiento | Llamada, trabajo, transcripción, evaluación de IA, worker e integración con Deepgram y Groq |

### 5.2 Ramas e integración

- `main` contiene el código integrado. Cada integrante trabaja en una rama propia (`feat/…`, `chore/…`) que parte de `main` actualizado.
- La integración se hace con Pull Request revisado por otro integrante. No se usa `push --force` sobre ramas compartidas.
- Los cambios al modelo de datos o a los contratos se acuerdan con el equipo y se reflejan en `fase1.md`.

### 5.3 Definition of Done verificada

- `dotnet build` sin errores ni advertencias nuevas y `dotnet test` en verde.
- Entidades empresariales con `IEntidadEmpresa` y FK compuestas `(empresa_id, id)`.
- Un solo contexto de empresa, un solo filtro global, un solo `ErrorDto` y un solo `PaginaDto`.
- Prueba de aislamiento entre empresas (404) en los endpoints nuevos.
- Ejemplos de cada endpoint en `backend/Qualia.API/Qualia.API.http`.
- Sin secretos ni archivos `.env` en los commits.

### 5.4 Enlaces del proyecto

**URL del repositorio:** https://github.com/BrayanPinel/Qualia  
**URL de GitHub Projects:** https://github.com/users/BrayanPinel/projects/2

---

## 6. Persistencia y Aislamiento Multiempresa

### 6.1 Entity Framework Core Code First

- El modelo se define en las entidades del dominio y en las clases `IEntityTypeConfiguration` de `Persistence/Configurations`, que se descubren automáticamente.
- La migración oficial `20261118151212_InitialCreate` crea las 24 tablas del esquema `qualia` desde una base vacía.
- En Development, la API aplica las migraciones al arrancar (`BaseDatos:AplicarEsquemaAlIniciar`) y siembra el catálogo de planes y los datos demo (`BaseDatos:SembrarDatosDemo`).
- Nombres en `snake_case`, enumeraciones guardadas como texto con restricción `CHECK`, fechas `timestamptz` en UTC y montos y minutos como `numeric`.

### 6.2 Global Query Filter por EmpresaId

Toda entidad empresarial implementa `IEntidadEmpresa`. `QualiaDbContext.AplicarFiltrosPorEmpresa` recorre el modelo y registra para cada una el filtro:

```csharp
e => e.EmpresaId == EmpresaIdActual
```

- `EmpresaIdActual` proviene de `IContextoEmpresa`. En una petición HTTP se toma del claim `empresa_id` del JWT validado; en el worker se fija con la empresa del trabajo en cola.
- **Falla cerrada:** sin empresa en el contexto, el filtro no devuelve filas.
- Una entidad nueva queda filtrada solo con implementar la interfaz; nadie escribe filtros manuales por empresa.
- Un recurso de otra empresa responde **404**. Un recurso de la misma empresa sin permiso responde **403**.

### 6.3 Excepciones justificadas al filtro

`IgnoreQueryFilters()` se usa solo donde el proceso necesita ver más de una empresa y siempre lleva un comentario:

| Uso | Motivo |
|---|---|
| Inicio de sesión | Buscar la identidad antes de conocer la empresa del usuario |
| Despachador automático y ejecutor del worker | Leer la cola de todas las empresas; el procesamiento se hace después con el filtro activo |
| Repositorio de suscripciones | Operaciones de consumo y plataforma que reciben la empresa explícitamente |
| Sembrador de datos demo | Precarga inicial de varias empresas |

### 6.4 Claves, restricciones e índices

- PK compuesta `(empresa_id, id)` en las tablas empresariales y FK compuestas entre ellas, más la FK `empresa_id → empresa(id)`.
- Unicidad: correo por empresa, versión de rúbrica por campaña, un trabajo, una evaluación y un consumo por llamada, y un criterio por evaluación.
- Índices únicos parciales: una rúbrica Activa por campaña, una suscripción vigente por empresa y una disputa Abierta por evaluación.
- Índices de consulta: `llamada(empresa_id, estado, rubrica_id)`, `revision_humana(empresa_id, evaluacion_id, fecha_revision)` y `trabajo_procesamiento(estado, proximo_intento)`.

### 6.5 Transacciones y concurrencia

- Las operaciones compuestas se ejecutan en una sola transacción: registro de empresa, encolado de una llamada (cambio de estado, reserva de minutos y trabajo) y cierre de la evaluación (evaluación, estado y facturación).
- Los contratos entre módulos reutilizan la transacción de quien los llama: pueden guardar, pero nunca confirmar ni abrir otra transacción.
- Las transiciones concurrentes usan `UPDATE … WHERE` condicionado (`ExecuteUpdateAsync`) o `SELECT … FOR UPDATE`, nunca "leer y luego escribir".
- La revisión humana usa concurrencia optimista con `revisionEsperada`; una revisión desactualizada responde 409 `REVISION_DESACTUALIZADA`.

---

## 7. API, Seguridad y Validación

### 7.1 Convenciones implementadas

- Prefijo `/api`, JSON en `camelCase` y enumeraciones como texto.
- Los campos desconocidos de entrada se rechazan (`UnmappedMemberHandling.Disallow`). La API nunca expone entidades de dominio, solo DTOs.
- Listados paginados con `pagina` desde 1 y `tamanoPagina` de 1 a 100 (predeterminado 20), respondidos como `PaginaDto<T>`.
- Las respuestas no incluyen hashes, credenciales de proveedores ni rutas internas de almacenamiento.

### 7.2 Autenticación con JWT

| Aspecto | Implementación |
|---|---|
| Emisión | `GeneradorTokenJwt` con `JsonWebTokenHandler` |
| Claims de empresa | `NameIdentifier` (usuario), `empresa_id` (empresa), `Role` y `Name` |
| Claims de plataforma | `NameIdentifier` y `Role = PlatformAdmin`, sin empresa |
| Firma | HMAC-SHA256 con la clave `Jwt__Key` del `.env` (mínimo 32 caracteres) |
| Validación | Emisor `qualia-api`, audiencia `qualia-web`, vigencia de 60 minutos y firma |
| Revocación | En cada petición se verifica que el usuario siga activo y la empresa no esté suspendida (`SESION_REVOCADA`) |
| Errores | 401 `NO_AUTENTICADO`, `TOKEN_EXPIRADO`, `TOKEN_INVALIDO` o `SESION_REVOCADA`; 403 `ROL_NO_AUTORIZADO`, todos con el formato `ErrorDto` |

El token está **firmado (JWS)**: el claim `empresa_id` no se puede alterar sin invalidar la firma, pero su contenido es legible en base64. Si la consigna exige cifrar el claim, se agrega `EncryptingCredentials` (JWE) al emitir el token, sin cambiar el resto del sistema.

En Development, con `Autenticacion:ModoDemo=true`, la API acepta además la cabecera `X-Usuario-Demo`, que usan las pruebas y los archivos `.http`. Fuera de Development ese esquema no se registra.

### 7.3 Contraseñas

- Hash con `PasswordHasher` de Microsoft: PBKDF2-HMAC-SHA512 con 210.000 iteraciones (recomendación de OWASP) y un mínimo configurable de 100.000.
- Correo y código de empresa se normalizan antes de guardar y de buscar.
- Empresa inexistente, correo inexistente y contraseña incorrecta responden el mismo 401. En los tres casos se calcula un hash para que el tiempo de respuesta no revele cuál falló.

### 7.4 Validación semántica con FluentValidation

1. Cada DTO de entrada tiene su validador (`AbstractValidator<T>`), registrado automáticamente con `AddValidatorsFromAssembly`.
2. El caso de uso ejecuta `ValidarOLanzarAsync`, que agrupa los errores por campo y lanza `ExcepcionNegocio` con código `VALIDACION` y estado 400.
3. `ManejadorErroresMiddleware` intercepta la excepción y responde el `ErrorDto`. También intercepta `FluentValidation.ValidationException` (por ejemplo, de `ValidateAndThrow`) y la responde con el mismo formato.
4. Los errores de enlace de modelo (JSON mal formado, tipos inválidos o campos desconocidos) se responden con el mismo `ErrorDto` mediante `RespuestaModeloInvalido`.

```json
{
  "status": 400,
  "codigo": "VALIDACION",
  "mensaje": "Uno o más campos no son válidos.",
  "errores": { "periodo": ["El periodo debe tener el formato YYYY-MM."] },
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

### 7.5 Endpoints implementados

| Método y ruta | Acceso | HU |
|---|---|---|
| GET /api/planes | Público | HU-06 |
| POST /api/empresas | Público | HU-01 |
| POST /api/auth/login | Público | HU-11 |
| POST /api/plataforma/auth/login | Público | HU-07 |
| GET /api/usuarios | AdminEmpresa | HU-12 |
| POST /api/usuarios | AdminEmpresa | HU-12 |
| PUT /api/usuarios/{id} | AdminEmpresa | HU-12 |
| GET /api/suscripcion | AdminEmpresa | HU-06 |
| GET /api/consumo?periodo=YYYY-MM | AdminEmpresa | HU-06 |
| POST /api/suscripcion/pago-simulado | AdminEmpresa | HU-06 |
| GET /api/plataforma/empresas | PlatformAdmin | HU-07 |
| PUT /api/plataforma/empresas/{empresaId}/suscripcion | PlatformAdmin | HU-07 |
| GET /api/campanas | AdminEmpresa; Supervisor; Agente; Auditor | HU-02 |
| POST /api/campanas | AdminEmpresa | HU-02 |
| PUT /api/campanas/{id} | AdminEmpresa | HU-02 |
| GET /api/campanas/{id}/usuarios | AdminEmpresa | HU-12 |
| PUT /api/campanas/{id}/usuarios/{usuarioId} | AdminEmpresa | HU-12 |
| DELETE /api/campanas/{id}/usuarios/{usuarioId} | AdminEmpresa | HU-12 |
| GET /api/campanas/{id}/rubricas | AdminEmpresa; Supervisor asignado | HU-02 |
| POST /api/campanas/{id}/rubricas | AdminEmpresa | HU-02 |
| PUT /api/rubricas/{id} | AdminEmpresa | HU-02 |
| POST /api/rubricas/{id}/publicar | AdminEmpresa | HU-02 |
| POST /api/llamadas/{id}/evaluacion | Supervisor asignado | HU-13 |
| GET /api/llamadas | AdminEmpresa; Supervisor asignado; Agente propietario | HU-03 |
| GET /api/llamadas/{id} | Mismos permisos de lectura | HU-03 |
| GET /api/llamadas/{id}/transcripcion | Mismos permisos de lectura | HU-03 |
| GET /api/llamadas/{id}/audio | Mismos permisos de lectura | HU-03 |
| GET /api/llamadas/{id}/evaluacion | Mismos permisos de lectura | HU-03 |
| POST /api/evaluaciones/{id}/confirmar | Supervisor asignado | HU-04 |
| PUT /api/evaluaciones/{id}/ajuste | Supervisor asignado | HU-04 |
| POST /api/evaluaciones/{id}/disputas | Agente propietario; plan habilitado | HU-05 |
| GET /api/disputas | AdminEmpresa; Supervisor asignado; Agente propietario | HU-05 |
| PUT /api/disputas/{id}/resolucion | Supervisor asignado | HU-05 |
| GET /api/agentes/me/scorecard | Agente | HU-09 |
| GET /api/agentes/{id}/scorecard | AdminEmpresa; Supervisor asignado | HU-10 |
| GET /api/campanas/{id}/scorecards | AdminEmpresa; Supervisor asignado | HU-10 |
| GET /api/reportes/campanas/{id} | AdminEmpresa; Supervisor asignado; Auditor | HU-10 |
| GET /api/audio-temporal/{token} | Anónimo con URL firmada (destino de `…/audio`) | HU-03 |

Cada endpoint está documentado en OpenAPI y Scalar, y tiene ejemplos ejecutables en `backend/Qualia.API/Qualia.API.http`.

### 7.6 Respuestas y errores

Todas las respuestas de error usan `ErrorDto { status, codigo, mensaje, errores?, traceId }`, definido una sola vez en `Application/Common/Errores`.

| Código HTTP | Uso | Códigos de negocio representativos |
|---|---|---|
| 400 | Campos o parámetros inválidos | `VALIDACION`, `RUBRICA_INVALIDA` |
| 401 | Sin autenticación válida | `NO_AUTENTICADO`, `CREDENCIALES_INVALIDAS`, `TOKEN_EXPIRADO`, `SESION_REVOCADA` |
| 403 | Rol, asignación, plan o suscripción no lo permiten | `ROL_NO_AUTORIZADO`, `CAMPANA_NO_ASIGNADA`, `FUNCION_NO_INCLUIDA`, `SUSCRIPCION_NO_ACTIVA` |
| 404 | Recurso inexistente o de otra empresa | `LLAMADA_NO_ENCONTRADA`, `CAMPANA_NO_ENCONTRADA`, `EVALUACION_NO_DISPONIBLE` |
| 409 | Duplicado, transición inválida, cuota o concurrencia | `CUOTA_AGOTADA`, `EVALUACION_YA_INICIADA`, `REVISION_DESACTUALIZADA`, `EVIDENCIA_INVALIDA`, `RUBRICA_PUBLICADA` |
| 500 | Error inesperado, sin detalles internos | `ERROR_INTERNO` |

Los casos de uso usan dos estilos compatibles: lanzar `ExcepcionNegocio`, o devolver `Resultado<T>` con `ErrorAplicacion` (módulo de suscripciones). El middleware y `ResultadoExtensions` producen el mismo `ErrorDto`, y `ResultadoExcepciones` permite pasar de un estilo al otro.

### 7.7 Ejemplo del flujo principal

1. Al arrancar, el worker encola las llamadas `Disponible` cuya rúbrica está publicada: cambia el estado a `EnCola`, reserva minutos y crea el trabajo en una transacción.
2. El worker transcribe, enmascara datos sensibles y evalúa cada criterio. Cada cita se valida contra el texto del segmento de la misma llamada.
3. Si no hay fallas, la evaluación es oficial (`oficial: true`). Si las hay, aparece en la bandeja del supervisor:

```http
GET /api/llamadas?requiereRevision=true
Authorization: Bearer <token>
```

4. El supervisor consulta la evaluación. Cada criterio incumplido trae el fragmento de la falta: cita, texto del segmento, hablante e instante del audio.

```http
GET /api/llamadas/10000000-0000-0000-0005-000000000001/evaluacion
Authorization: Bearer <token>
```

5. El supervisor confirma la evaluación o la ajusta enviando la revisión esperada:

```http
POST /api/evaluaciones/{id}/confirmar
Authorization: Bearer <token>
Content-Type: application/json

{ "revisionEsperada": 1 }
```

6. El scorecard del agente y el reporte de la campaña incluyen la evaluación en cuanto es oficial y no tiene una disputa abierta.

---

## 8. Motor de Evaluación con IA y Verificación

### 8.1 Procesamiento asíncrono

| Componente | Responsabilidad |
|---|---|
| `DespachadorEvaluacionAutomatica` | Encola las llamadas nuevas. Pausa un minuto a la empresa sin suscripción activa o sin cuota |
| `EncoladorEvaluacion` | Transacción de encolado, compartida por el despachador y el inicio manual |
| `EjecutorProcesamiento` | Reclama trabajos de forma atómica y los procesa con el contexto de la empresa del trabajo |
| `ProcesadorTrabajo` | Fases idempotentes: transcripción, evaluación y cierre con facturación |
| `ConstructorEvaluacion` | Valida cobertura, puntajes y citas; marca la evaluación para revisión cuando corresponde |
| `WorkerProcesamiento` | `BackgroundService` que ejecuta el despachador y el ejecutor en bucle |

- **Reintentos limitados:** tres intentos con espera exponencial. El motivo del fallo se sanea antes de exponerse.
- **Fallo definitivo:** la llamada queda `Fallida` y el consumo se libera. El supervisor puede volver a evaluarla con el mismo trabajo y el mismo consumo.
- **Enmascarado (RN-14):** correos, teléfonos, tarjetas y documentos se reemplazan antes de guardar o enviar el texto a la IA.
- **Modos:** `IA:Modo=Real` usa Deepgram y Groq; `Simulado` produce resultados deterministas sin red y se usa si faltan las llaves.

### 8.2 Pruebas automatizadas

| Tipo | Cobertura principal |
|---|---|
| Unitarias | Dominio, rúbricas, construcción de la evaluación, enmascarado, hash, JWT, URL firmada, proveedores de IA, middleware de errores |
| Integración (API real + PostgreSQL con Testcontainers) | Registro, autenticación, usuarios, aislamiento, campañas y rúbricas, llamadas, reintentos, evaluación automática, revisión humana, disputas, consumo real, desempeño y migraciones |

**Resultado actual:** 218 pruebas, 217 aprobadas y 1 omitida a propósito (la prueba con IA real, que consume crédito y se activa con `QUALIA_PRUEBAS_IA_REAL=1`).

```powershell
cd backend; dotnet test      # requiere Docker encendido
```

### 8.3 Ejecución local

```powershell
Copy-Item infra\example.env infra\.env
Copy-Item backend\Qualia.API\example.env backend\Qualia.API\.env   # completar claves
cd infra; docker compose up -d --wait
cd ..\backend\Qualia.API; dotnet run                                # http://localhost:5008 → /scalar/v1
```

---

### Fuentes de la entrega

- **Proyecto Final:** requisitos y rúbrica de la fase 2.
- **Qualia, fase 1** (`docs/fase1.md`): historias, reglas de negocio, modelo de datos y contratos de interfaz.
- **Repositorio Qualia:** código fuente, migración `InitialCreate`, pruebas y ejemplos en `Qualia.API.http`.
