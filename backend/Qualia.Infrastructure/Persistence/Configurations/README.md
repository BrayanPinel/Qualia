# Configuraciones EF Core

Cada rol agrega aquí sus clases `IEntityTypeConfiguration<T>` para las entidades
que le corresponden. `QualiaDbContext` las descubre automáticamente mediante
`ApplyConfigurationsFromAssembly`.

Reglas acordadas en A00:

- No editar `QualiaDbContext` para agregar configuraciones individuales.
- No generar ni modificar migraciones sin coordinación con el responsable de migraciones.
- No editar manualmente `QualiaDbContextModelSnapshot.cs`.
- Mantener los nombres de tablas, columnas, claves e índices del diagrama aprobado.
