# Qualia: frontend

React 19 + TypeScript + React Compiler (Vite), Tailwind CSS 4, Axios, Zod + React Hook Form y React Router.
La división del trabajo y las reglas del equipo están en `fase3-division-trabajo.md`.

## Arranque

```powershell
cd frontend
pnpm install
copy example.env .env     # VITE_API_BASE_URL apunta a la API local
pnpm run dev              # http://localhost:5173
```

Antes de abrir un PR: `pnpm run lint` y `pnpm run build` sin errores.

## Estructura

```plaintext
src/
├── App.tsx        Orquestador: solo declara las rutas
├── layouts/       AppLayout (sidebar + cabecera móvil)
├── shared/        apiClient, rutaEmpresa, extractor de errores, componentes base y tipos comunes
└── features/      Un módulo por área, cada uno con api/, components/, schemas/ y types/
    ├── auth, empresas, usuarios, plataforma      Integrante 1
    ├── campanas, revision, disputas              Integrante 2
    └── llamadas, desempeno, suscripcion          Integrante 3
```

`shared/`, `App.tsx` y `layouts/` los modifica solo el Integrante 1; los demás piden el cambio en su PR.
Los `.gitkeep` solo mantienen las carpetas vacías en Git: se pueden borrar al crear el primer archivo.
