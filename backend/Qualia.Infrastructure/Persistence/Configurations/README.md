# Configuraciones EF Core

Cada rol agrega aquí sus clases `IEntityTypeConfiguration<T>` para las entidades
que le corresponden. `QualiaDbContext` las descubre automáticamente mediante
`ApplyConfigurationsFromAssembly`.

Reglas acordadas en A00:

- No editar `QualiaDbContext` para agregar configuraciones individuales (solo `DbSet`).
- Una sola persona a la vez genera migraciones; avisar al equipo antes para que no choquen los snapshots.
- No editar manualmente `QualiaDbContextModelSnapshot.cs` ni una migración ya fusionada en `main`.
- Mantener los nombres de tablas, columnas, claves e índices del diagrama aprobado.

## Migraciones

La migración oficial `InitialCreate` (24 tablas) está en `Persistence/Migrations/`. En Development la API
la aplica sola al arrancar (`BaseDatos:AplicarEsquemaAlIniciar`).

Si cambias entidades o configuraciones, agrega una migración nueva desde `backend/`:

```powershell
dotnet tool restore
dotnet ef migrations add NombreDelCambio --project Qualia.Infrastructure --startup-project Qualia.API --output-dir Persistence/Migrations
dotnet ef migrations script --project Qualia.Infrastructure --startup-project Qualia.API   # revisar el SQL
```

La prueba `MigracionesIntegracionTests.El_modelo_no_tiene_cambios_sin_migrar` falla si el modelo cambió
y falta la migración.

Una base local creada antes con `EnsureCreated` no se puede migrar: la API se detiene con la instrucción
para recrearla una sola vez (`cd infra; docker compose down -v; docker compose up -d --wait`).
