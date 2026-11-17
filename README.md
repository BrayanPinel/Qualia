# Qualia

> **La IA propone, el humano decide.**

Qualia es una plataforma web SaaS B2B dirigida a centros de contacto para gestionar la evaluación de calidad de llamadas mediante rúbricas configurables, asistencia de IA y validación humana. El sistema garantiza un aislamiento estricto de los datos de cada empresa (arquitectura Multi-tenant).

---

## Datos Academicos
* **Asignatura:** ISC-305 Programación Web
* **Proyecto:** Plataforma SaaS Multi-empresa (Proyecto Final)
* **Integrantes:** 
  * Brayan Pinel
  * Jonathan Ordoñez
  * Javier Flores
  * Marcel Euceda

---

## Stack Tecnológico y Arquitectura

Este proyecto respeta los requerimientos del _Zero-Cost Stack_ y las prácticas vistas en clases de ingeniería de software:

### Backend
* **Tecnologías:** C# / .NET, PostgreSQL, Entity Framework Core (Code-First).
* **Arquitectura:** Clean Architecture (Domain, Application, Infrastructure, API) con aislamiento Multi-empresa vía Global Query Filters.
* **Seguridad:** Autenticación con Tokens JWT (EmpresaId encriptado como Claim).
* **Validación:** FluentValidation propagado por Middleware global.

### Frontend
* **Tecnologías:** React, TypeScript, Axios, react-router-dom.
* **Arquitectura:** Vertical Slicing (agrupación por funcionalidades).
* **Validación:** Zod + React Hook Form (en simetría con el backend).

### DevOps & Despliegue Cloud
* **Local:** Docker Multi-stage y Docker Compose efímero.
* **Producción (Cloud gratuita):** Microsoft Azure

---

## Estructura del Repositorio

El proyecto se divide en dos aplicaciones principales separadas físicamente:

```text
/
├── .github/           # Plantillas de Issues y flujos de GitHub Actions
├── backend/           # Solución .NET bajo Clean Architecture
├── frontend/          # Aplicación React/Vite bajo Vertical Slicing
├── docs/              # Documentación, diagramas ERD y contratos de interfaz
└── infra/             # Docker para la orquestación local para desarrollo
