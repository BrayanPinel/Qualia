# Qualia
**Plataforma SaaS multiempresa de evaluación de calidad de conversaciones con inteligencia artificial**

**Asignatura:** ISC-305 Seccion 1700
**Grupo2**
**Integrantes:** Brayan Pinel, Jonathan Ordoñez, Javier Flores, Marcel Euceda

## 1. Visión Global
**Qualia: la IA propone, el humano decide.**

Los centros de contacto necesitan evaluar la calidad de sus conversaciones para identificar errores, verificar el cumplimiento de sus procedimientos y mejorar la atención. Sin embargo, escuchar y calificar llamadas manualmente consume tiempo y limita la cantidad de conversaciones revisadas. Además, cuando la retroalimentación llega sin evidencia concreta, resulta difícil explicar una calificación y orientar al agente sobre cómo mejorar.

**Qualia** es una plataforma web SaaS B2B dirigida a centros de contacto pequeños y medianos, equipos de soporte, ventas y cobranza. Cada empresa dispone de un espacio privado donde administra sus usuarios, campañas y criterios de calidad. La empresa es quien contrata la suscripción; sus supervisores y agentes utilizan el sistema como usuarios de esa organización.

La plataforma recibe grabaciones de llamadas, las transcribe, enmascara datos sensibles en el texto y genera un borrador de evaluación mediante inteligencia artificial. La evaluación utiliza una rúbrica configurable y versionada por campaña. Cada criterio puede vincularse con un segmento de la transcripción y con el instante correspondiente del audio, lo que permite al supervisor revisar la evidencia sin escuchar nuevamente toda la conversación.

Las evaluaciones validadas alimentan un scorecard individual con puntaje, resultados por criterio y seguimiento del agente. Las funciones de tendencia, comparación por campaña, disputas y coaching se habilitan según el plan. Solo se muestran promedios oficiales cuando existe una cantidad mínima de llamadas confirmadas, y las evaluaciones con disputas abiertas se excluyen temporalmente del cálculo.

El modelo comercial consiste en una suscripción mensual por empresa con minutos de audio incluidos y límites de uso. Los planes propuestos son Básico, Pro y Enterprise. El consumo por minuto permite relacionar el servicio ofrecido con el volumen de conversaciones procesadas. Para el proyecto académico, el cobro se simula y se utilizan audios ficticios. El modo de IA simulado permite demostrar el flujo completo sin depender de créditos de proveedores.

### 1.1 Objetivo general
Diseñar una plataforma SaaS multiempresa para gestionar la evaluación de calidad de llamadas mediante rúbricas configurables, asistencia de IA y validación humana, garantizando el aislamiento de los datos de cada empresa.

| Área | Decisión de diseño para las fases posteriores |
|---|---|
| Backend | C#/.NET con Domain, Application, Infrastructure y API |
| Organización | Vertical Slicing por funcionalidades |
| Frontend | React y TypeScript; formularios con React Hook Form y Zod |
| Persistencia | PostgreSQL y Entity Framework Core |
| Seguridad | Autenticación JWT y autorización por roles y recursos |
| Integraciones | Interfaces para transcripción, evaluación, almacenamiento y pagos simulados |


### Funcionalidades incluidas

1. Registro de empresa y administrador inicial, con selección de plan y suscripción simulada.
2. Inicio de sesión, gestión de usuarios y permisos por rol.
3. Gestión de campañas y asignación de agentes y supervisores.
4. Rúbricas versionadas con criterios, pesos e indicadores críticos.
5. Carga manual de audios ficticios y consulta de su estado de procesamiento.
6. Procesamiento asíncrono: transcripción, enmascarado, evaluación y validación de evidencia.
7. Consulta de transcripción y reproducción del fragmento asociado a un criterio.
8. Confirmación o ajuste humano, con registro de los cambios.
9. Disputas, scorecards y coaching, respetando las funciones del plan.
10. Consulta de suscripción y consumo; bloqueo de nuevas cargas cuando corresponda.
11. Administración comercial por PlatformAdmin, sin acceso al contenido de llamadas.
12. Modo simulado de IA y planificación de verificaciones de aislamiento.

### Actores y permisos

| Rol | Responsabilidades | Restricción principal |
|---|---|---|
| PlatformAdmin | Gestionar empresas, planes y suscripciones; consultar consumo global | Sin acceso a audios, transcripciones ni evaluaciones individuales |
| AdminEmpresa | Gestionar usuarios, campañas, rúbricas, asignaciones y suscripción; consultar resultados | Solo su empresa; no confirma evaluaciones por este rol |
| Supervisor | Cargar llamadas, revisar evidencia, confirmar o ajustar, resolver disputas y crear coaching | Solo campañas asignadas |
| Agente | Consultar sus llamadas, calificaciones, scorecard y coaching; abrir disputas | Solo información propia |
| Auditor | Consultar reportes agregados | Solo su empresa; sin audio ni scorecards individuales |

Para el MVP se adopta la restricción más específica del documento base para Auditor: reportes agregados. No se habilita la consulta de evaluaciones individuales.

### Planes y suscripción

| Plan propuesto | Minutos mensuales | Supervisores | Rúbricas activas | Funciones |
|---|---:|---:|---:|---|
| Básico | 1,000 | 2 | 1 | Evaluación y scorecard básico |
| Pro | 5,000 | 10 | Sin límite | Básico más tendencia, comparación, disputas y coaching |
| Enterprise | 20,000 | Sin límite | Sin límite | Pro más prioridad de procesamiento y retención extendida |

Propuestas de precios

| Estado | Regla de acceso |
|---|---|
| Prueba | Permite operar dentro de la cuota de prueba |
| Activa | Permite operar según el plan |
| EnMora | Permite consulta; bloquea nuevas cargas |
| Vencida | Permite consulta durante el periodo de gracia configurado; bloquea cargas |

| ID | Regla |
|---|---|
| RN-01 | Toda operación empresarial se ejecuta dentro de la empresa autenticada. |
| RN-02 | El email es único dentro de cada empresa; el login identifica empresa y credenciales. |
| RN-03 | La campaña, el agente, la rúbrica y la llamada deben pertenecer a la misma empresa. El agente debe estar asignado a la campaña. |
| RN-04 | Una rúbrica publicada es inmutable. Cada cambio crea una versión nueva; una campaña tiene como máximo una versión activa. |
| RN-05 | Los pesos de los criterios son positivos y suman 100 al publicar. Cada nota está entre 0 y 100. |
| RN-06 | El total se calcula como suma de `nota × peso / 100`. Los fallos críticos se reportan aparte; no provocan una sanción automática. |
| RN-07 | La llamada conserva la versión de rúbrica seleccionada al ingresar, aunque después cambie la versión activa de la campaña. |
| RN-08 | El servidor calcula notas totales y consumo. El cliente no puede fijarlos. |
| RN-09 | Una cita debe existir en un segmento de la misma llamada. Los tiempos provienen del segmento, nunca de la generación del LLM. |
| RN-10 | La ausencia de evidencia se representa explícitamente. Una evaluación con evidencia inválida exige revisión humana antes de oficializarse. |
| RN-11 | Solo las evaluaciones confirmadas o ajustadas, sin disputa abierta, cuentan en el promedio oficial. Mínimo predeterminado: 5 llamadas elegibles por campaña y periodo. |
| RN-12 | Cada ajuste exige motivo; se conserva el resultado propuesto por IA y el resultado humano. |
| RN-13 | La carga reserva minutos de forma atómica; los reintentos del mismo trabajo no duplican la reserva ni el consumo facturable. |
| RN-14 | Solo se usan audios ficticios en la demostración. La transcripción se enmascara antes de almacenarse y enviarse al evaluador. |

---

---

## 3. Épicas del Sistema

| Épica | Nombre | Alcance |
|---|---|---|
| EP-01 | Identidad y seguridad | Empresa, login, roles, usuarios y aislamiento |
| EP-02 | Administración comercial | Planes, suscripciones y límites |
| EP-03 | Campañas y rúbricas | Campañas, asignaciones y versiones de evaluación |
| EP-04 | Motor de evaluación | Carga, cola, transcripción y evaluación asistida |
| EP-05 | Revisión humana | Evidencia, confirmación, ajustes y disputas |
| EP-06 | Desempeño individual | Scorecard, resultados por campaña y coaching |

---
