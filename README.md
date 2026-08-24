# PROCOFA — Sistema Web de Gestión de Auditorías OEA/C-TPAT

Backend del sistema de gestión de auditorías OEA/C-TPAT de PROCOFA (Etapa 1: licencia
de uso interno). Digitaliza el ciclo PHVA de auditorías — Planeación, Ejecución,
Hallazgos, Acciones correctivas, Cierre, Reporte — reemplazando procesos manuales por
flujos automatizados con generación de reportes en Word/PDF.

## Propósito de este repositorio

Backend en .NET, construido contra una base de datos PostgreSQL **ya existente**
(ver advertencia más abajo). El frontend (React 18 + TypeScript + Vite) vive en un
repositorio separado.

## Arquitectura

Clean Architecture / Hexagonal (Ports & Adapters), 4 capas con una única dirección de
dependencia:

```
Procofa.Domain            (sin dependencias externas ni a otros proyectos PROCOFA)
        ↑
Procofa.Application        (→ Domain — puertos y casos de uso)
        ↑
Procofa.Infrastructure      (→ Application, → Domain — implementa los puertos)
        ↑
Procofa.Api                  (→ Application; → Infrastructure solo en el Composition Root)
```

- **Domain**: entidades, value objects, invariantes, domain events. Sin ASP.NET Core,
  sin EF Core, sin Npgsql.
- **Application**: casos de uso (Command/Query) y los puertos que Infrastructure debe
  implementar (`IEvidenceStorage`, `ITenantUnitOfWork`, repositorios, etc.).
- **Infrastructure**: EF Core (`ProcofaDbContext`), tenancy/RLS, storage, generación de
  reportes, Outbox, autenticación (JWT, `PasswordHasher`).
- **Api**: Controllers/endpoints, middleware, autorización, `Program.cs` como único
  Composition Root que conoce Application e Infrastructure a la vez.

No existen capas genéricas (`Manager`, `Service` genérico, `GenericRepository`,
`IRepository<T>`): cada aggregate root tiene el puerto de persistencia específico que
realmente necesita, cuando lo necesita.

El detalle completo de agregados, casos de uso y decisiones congeladas está en
[`docs/PROCOFA_AUDIT_BASELINE_V2_1.md`](docs/PROCOFA_AUDIT_BASELINE_V2_1.md).

## Estado actual (Foundation)

Este repositorio contiene únicamente la **Foundation**: estructura de solución,
referencias entre proyectos, configuración de build, y un endpoint técnico `/health`.
Deliberadamente **no** contiene todavía: entidades de Domain, casos de uso, modelo EF
Core, migraciones, autenticación, RLS, ni ningún endpoint funcional. Eso llega en las
siguientes instrucciones de implementación (empezando por Persistencia EF Core +
representación del baseline V2.1 + Tenant/RLS).

## ⚠️ Advertencia — la base de datos V2.1 ya existe

**La base PostgreSQL V2.1 ya existe y contiene 48 tablas, RLS multitenant, triggers y
funciones ya definidos y aprobados como base arquitectónica** (ver
`docs/PROCOFA_AUDIT_BASELINE_V2_1.md`). Ninguna migración de EF Core debe intentar
recrear ese esquema:

- **No** ejecutar `dotnet ef database update` con una migración `InitialCreate`
  generada por scaffold.
- **No** ejecutar `dotnet ef migrations add` sin antes revisar el SQL generado
  (`dotnet ef migrations script`) contra el esquema real.
- **No** modificar `procofa_bdFinal.sql` (dump físico de referencia) ni ejecutar
  comandos de creación de base sin seguir el procedimiento de baseline del proyecto.
- Toda migración de EF Core se ejecuta con una credencial administrativa/de migración
  separada — el rol de runtime `procofa_app` nunca recibe privilegios DDL.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/) (versión exacta fijada en
  [`global.json`](global.json))
- PostgreSQL no es necesario todavía para compilar/testear esta Foundation (ver
  advertencia arriba); será necesario a partir de la instrucción de Persistencia.

## Comandos

```bash
# Restaurar dependencias
dotnet restore

# Compilar toda la solución (0 warnings — TreatWarningsAsErrors=true)
dotnet build

# Ejecutar toda la suite de tests
dotnet test

# Levantar la Api en local — puerto explícito, no depende de launchSettings.json
dotnet run --project src/Procofa.Api --urls http://localhost:5188

# Verificar el endpoint técnico de salud
curl http://localhost:5188/health
# => {"status":"Healthy"}
```

## Estructura del repositorio

```
Procofa.sln
Directory.Build.props        # TargetFramework/Nullable/ImplicitUsings/TreatWarningsAsErrors comunes
Directory.Packages.props     # Central Package Management — versiones de NuGet centralizadas
global.json                  # SDK .NET fijado
.editorconfig
.gitignore
src/
├── Procofa.Domain
├── Procofa.Application
├── Procofa.Infrastructure
└── Procofa.Api
tests/
├── Procofa.Domain.Tests
├── Procofa.Application.Tests
├── Procofa.IntegrationTests
└── Procofa.Api.Tests
docs/
└── PROCOFA_AUDIT_BASELINE_V2_1.md
```

## Gestión centralizada de paquetes

Todas las versiones de paquetes NuGet se fijan una sola vez en
`Directory.Packages.props` (`ManagePackageVersionsCentrally`); los `.csproj`
individuales solo declaran `<PackageReference Include="..." />`, sin versión.
