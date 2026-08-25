# Procedimiento `InitialBaseline` — documentado, NO ejecutado

**Por qué está documentado y no ejecutado:** `dotnet ef migrations add`
requiere el paquete `Microsoft.EntityFrameworkCore.Design` ya restaurado
(`dotnet restore`), y este sandbox no tiene salida a `api.nuget.org`
(`NU1301` confirmado repetidamente — ver sección J/L del reporte de
Instrucción 03). El código está escrito y listo para compilar; este
documento es el procedimiento exacto a seguir cuando alguien con acceso a
NuGet ejecute los comandos reales.

**Regla de seguridad que gobierna todo este documento:** ningún comando de
esta página se ejecuta jamás contra `procofa_audit_db` real. Todo Fase A-D
ocurre contra una instancia PostgreSQL 18 desechable (Testcontainers o un
contenedor local efímero). Ver también la sección "Qué hacer con la BD
real" al final.

## Contexto: dos mecanismos distintos, no confundir

1. **Bootstrap de tests** (usado por los ~16 integration tests de
   `Procofa.IntegrationTests`): carga `db/baseline/v2.1/001_schema.sql`,
   `002_security.sql` y `003_seed_catalogs.sql` DIRECTAMENTE vía SQL crudo
   (Npgsql) contra el contenedor Testcontainers recién creado. NO usa el
   mecanismo de migraciones de EF Core. Es el camino rápido y determinista
   para cada test run.

2. **Migración `InitialBaseline`** (este documento): el mecanismo formal de
   EF Core para que, a partir de ahora, los CAMBIOS futuros al esquema se
   gestionen vía `dotnet ef migrations add <Nombre>` de forma incremental.
   Como la BD real (`procofa_audit_db`) YA EXISTE con el esquema completo
   (no se está creando desde cero), esta migración se "hornea vacía"
   (Fase C) — existe solo para que EF sepa "el modelo actual ya está
   reflejado en la BD real", sin intentar re-crear nada.

Estos dos mecanismos son independientes: los tests NO dependen de que
`InitialBaseline` exista, y `InitialBaseline` no se usa para levantar la BD
de los tests.

## Fase A — Generar la migración

```bash
cd src/Procofa.Infrastructure
dotnet ef migrations add InitialBaseline \
    --startup-project ../Procofa.Api \
    --output-dir Persistence/Migrations
```

Esto requiere que `Procofa.Api` tenga un `DbContext` resoluble en tiempo de
diseño — ya lo tiene, vía `AddInfrastructure(...)` en `Program.cs`
(Instrucción 03). Si `dotnet ef` no logra resolver el `DbContext` por no
encontrar una connection string real en tiempo de diseño, el placeholder
que `DependencyInjection.AddInfrastructure` usa cuando la connection string
está ausente es suficiente — `dotnet ef migrations add` NUNCA abre una
conexión real, solo necesita construir el modelo.

**Resultado esperado:** 3 archivos nuevos en
`Persistence/Migrations/`:
- `<timestamp>_InitialBaseline.cs` — `Up()`/`Down()` con `CreateTable(...)`
  para las 42 tablas con `DbSet` propio + las 6 tablas de tipos poseídos
  (`OwnsMany`), más todos los `CreateIndex`/`AddForeignKey`/
  `AddCheckConstraint`.
- `<timestamp>_InitialBaseline.Designer.cs` — snapshot serializado de ESTE
  punto en el tiempo (metadata interna de EF, no se edita a mano).
- `ProcofaDbContextModelSnapshot.cs` — snapshot del modelo COMPLETO
  actual (se actualiza automáticamente en cada `migrations add` futuro).

## Fase B — Comparar contra el baseline físico, corregir SOLO las configuraciones

**Nunca editar a mano el SQL generado ni el `.Designer.cs`.** Si el
`Up()` generado no coincide exactamente con `db/baseline/v2.1/001_schema.sql`
(nombre de columna, tipo, default, longitud, `CHECK`, índice, FK), el
error está en la `IEntityTypeConfiguration<T>` correspondiente — se corrige
ahí, se borra la migración (`dotnet ef migrations remove`) y se vuelve a
generar (Fase A) hasta que coincida. Esto es intencional: las 42+6
configuraciones en `Persistence/Configurations/` son la ÚNICA fuente de
verdad editable; la migración es siempre un DERIVADO, nunca al revés.

Checklist de comparación (usar el reporte de "Schema Parity" de la
Instrucción 03 como punto de partida, sección D del reporte):
- 48 tablas presentes, ningún nombre físico distinto.
- Tipos/longitud/precisión exactos (`varchar(N)`, `numeric(p,s)`, `text`,
  `jsonb`, `date`, `timestamptz`, `inet`).
- Los 3 `lock_version` como `bigint DEFAULT 1 NOT NULL` +
  `IsConcurrencyToken`.
- Los 6 `CHECK` de enums VARCHAR con los valores exactos.
- Las 6 particiones de índice parcial (`HasFilter`) con el `WHERE`
  correcto.
- Los 126 FK con el `ON DELETE` correcto (¡ojo con los 3 casos `RESTRICT`
  que rompen el patrón `CASCADE` habitual: `report_templates`/
  `report_template_versions` → `tenants`, y `audit_logs` → `tenants`!).

## Fase C — Vaciar `Up()`/`Down()`, conservar el `ModelSnapshot`

Una vez que el `Up()` generado en Fase A/B coincide EXACTAMENTE con el
baseline físico (validado en Fase D contra una BD vacía — ver abajo),
se reemplaza el CUERPO de los métodos por no-ops, dejando la firma:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Intencionalmente vacío: procofa_audit_db YA TIENE este esquema
    // (fue creado manualmente / vía los scripts de
    // db/baseline/v2.1/*.sql antes de que EF Core gestionara el
    // ciclo de vida del esquema). Esta migración existe únicamente
    // para que EF Core sepa que el modelo representado por
    // ProcofaDbContextModelSnapshot.cs, en este punto, YA está
    // reflejado en la base de datos real — sin volver a emitir
    // CREATE TABLE/CREATE INDEX/etc. Las migraciones FUTURAS
    // (dotnet ef migrations add <Siguiente>) diffean correctamente
    // contra el snapshot completo, que NO se toca.
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // Intencionalmente vacío — ver Up(). No hay "deshacer" un baseline.
}
```

**El `ModelSnapshot` (`ProcofaDbContextModelSnapshot.cs`) NO se toca** —
debe conservar la representación completa del modelo, generada
automáticamente en Fase A. Solo el archivo `<timestamp>_InitialBaseline.cs`
se edita (vaciar los dos métodos).

## Fase D — Validar, dos caminos distintos y complementarios

### D.1 — Prueba de fidelidad (BD vacía, `Up()` SIN vaciar)

Antes de vaciar `Up()`/`Down()` (es decir, ANTES de Fase C, con el commit
de la migración recién generada en Fase A/B), aplicar la migración
completa contra una PostgreSQL 18 Testcontainers **vacía** (sin los
scripts de `db/baseline/` precargados):

```bash
dotnet ef database update InitialBaseline \
    --connection "Host=localhost;Port=<puerto-testcontainers>;Database=procofa_fidelity_check;Username=postgres;Password=postgres"
```

Si esto corre sin error, es la prueba más fuerte de que el modelo EF
representa fielmente el baseline físico — cualquier `CHECK`, FK, índice o
tipo mal mapeado hace fallar el `CREATE TABLE`/`ALTER TABLE`
correspondiente aquí. Comparar después el catálogo resultante
(`information_schema.columns`, `pg_indexes`, `pg_constraint`) contra
`db/baseline/v2.1/001_schema.sql` — deben coincidir 1:1.

### D.2 — Prueba de baseline-tracking (BD con `db/baseline/` precargado, `Up()` YA vacío)

Con `Up()`/`Down()` ya vacíos (Fase C aplicada), contra una segunda
instancia Testcontainers en la que YA se cargaron
`001_schema.sql` → `002_security.sql` → `003_seed_catalogs.sql` (el mismo
bootstrap que usan los integration tests):

```bash
dotnet ef database update InitialBaseline \
    --connection "Host=localhost;Port=<puerto-testcontainers>;Database=procofa_baseline_tracking;Username=postgres;Password=postgres"
```

Como `Up()` es un no-op, esto NO debe fallar aunque las tablas ya existan
— el único efecto real es que EF Core inserta la fila correspondiente en
`__EFMigrationsHistory`. Verificar:

```sql
SELECT * FROM "__EFMigrationsHistory";
-- Debe mostrar exactamente 1 fila: MigrationId = '<timestamp>_InitialBaseline',
-- ProductVersion = la versión de Microsoft.EntityFrameworkCore usada (10.0.11).
```

Este es el procedimiento que se ejecutaría, UNA SOLA VEZ, contra
`procofa_audit_db` real cuando el equipo decida empezar a gestionar su
esquema vía EF Core Migrations desde este punto en adelante.

## Qué hacer con la BD real (`procofa_audit_db`)

Fuera de alcance de Instrucción 03 (requiere aprobación explícita y
ejecución supervisada, nunca por Claude en este sandbox):

1. Congelar cambios manuales al esquema de `procofa_audit_db`.
2. Ejecutar D.2 contra un **snapshot/réplica** de `procofa_audit_db`
   primero (nunca directo contra producción) para confirmar 0 filas
   afectadas y 1 fila nueva en `__EFMigrationsHistory`.
3. Solo entonces, con aprobación explícita, ejecutar el mismo comando
   contra `procofa_audit_db` real.
4. A partir de ahí, todo cambio de esquema futuro pasa exclusivamente por
   `dotnet ef migrations add <Nombre>` + revisión (Fase A-B de un nuevo
   ciclo) — nunca `ALTER TABLE` manual.
