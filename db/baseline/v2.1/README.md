# Baseline PostgreSQL V2.1 — scripts reproducibles y sanitizados

Origen: `procofa_bdFinal.sql` (dump PGDMP custom-format real de
`procofa_audit_db`, `server_version=18.3`, 522 entradas de TOC), parseado con
`pgdumplib` y contrastado contra `docs/PROCOFA_AUDIT_BASELINE_V2_1.md`
(Instrucción 03, Paso 1-2 — coincidencia exacta en todas las dimensiones:
48 tablas, 126 FK, 73 PK/UNIQUE, 36 índices, 54 triggers, 6 funciones,
36 tablas RLS/36 policies, 50 ACL, 2 extensiones, 13 catálogos seed).

Estos tres scripts son la **única** forma soportada de reconstruir el
esquema para pruebas — nunca se ejecutan contra `procofa_audit_db` real ni
contra ningún host que no sea una instancia PostgreSQL 18 desechable
(Testcontainers en integration tests, o un contenedor local efímero para
depuración manual).

## Qué se sanitizó y por qué

- **`001_schema.sql`** excluye el `CREATE DATABASE` original — usaba
  `LOCALE = 'Spanish_Mexico.1252'` (específico de Windows, no existe en la
  imagen `postgres:18` de Linux que usan Testcontainers/CI). El motor
  destino usa su propio locale por defecto; ninguna columna ni collation
  del esquema depende de un locale específico, así que esto no afecta la
  fidelidad estructural.
- **`002_security.sql`** no contiene ningún secreto real: las contraseñas
  de `procofa_owner`/`procofa_app` son placeholders de prueba
  (`test_only_owner_pw` / `test_only_app_pw`) para un contenedor efímero
  que nunca se expone en red — no son las credenciales reales de
  producción (que este proyecto no conoce ni necesita conocer).
- **`003_seed_catalogs.sql`** contiene ÚNICAMENTE catálogos de referencia
  (roles, permisos, catálogos de estado/tipo/prioridad, y el tenant fijo
  de Etapa 1: `00000000-0000-0000-0000-000000000001` / PROCOFA). Cero
  datos transaccionales, cero PII de clientes o usuarios reales, cero
  contraseñas de usuario.

Ningún archivo de este directorio fue copiado a ciegas: cada uno se generó
programáticamente a partir de los fragmentos ya extraídos y verificados del
dump real (evita transcripción manual de ~2600 líneas de SQL), y luego se
revisó que no colara nada de lo excluido arriba.

## Orden de ejecución

```
1. 001_schema.sql     -- extensiones, 48 tablas (con FORCE ROW LEVEL
                          SECURITY inline), PK/UNIQUE, FK, índices,
                          funciones PL/pgSQL, triggers.
2. 002_security.sql   -- crea procofa_owner/procofa_app, reasigna
                          ownership de las tablas a procofa_owner,
                          ENABLE ROW LEVEL SECURITY (36 tablas), las 36
                          policies, y el ACL real (GRANT/REVOKE).
3. 003_seed_catalogs.sql -- 13 tablas de catálogo (95 filas), idempotente
                          (ON CONFLICT DO NOTHING).
```

Los tres deben ejecutarse **en ese orden**, como el superusuario por
defecto del contenedor (`postgres` en la imagen oficial `postgres:18`).
`002_security.sql` depende de que las tablas de `001_schema.sql` ya
existan; `003_seed_catalogs.sql` depende de los roles/tablas de los dos
anteriores (aunque el propio `INSERT` se ejecuta igual como superusuario,
no como `procofa_app`, para no quedar sujeto a RLS durante el seed).

## Cómo deben conectarse los tests

Las queries de los tests de integración (Testcontainers) que ejercen
RLS/ACL de verdad **deben** abrir su conexión como `procofa_app` —
nunca como el superusuario del contenedor ni como `procofa_owner`. Un
test corriendo como superusuario o como el owner de la tabla nunca puede
demostrar fail-closed real, porque RLS/ACL no aplican de la misma forma
(ver `FORCE ROW LEVEL SECURITY` — está pensado exactamente para que ni
siquiera el owner escape la policy, pero el patrón de prueba más
representativo de producción sigue siendo ejercer todo a través de
`procofa_app`, el rol que la aplicación real usa).

## Qué NO es este directorio

No es un reemplazo de `procofa_bdFinal.sql` como fuente de verdad física
(sigue siendo la referencia #1 según Instrucción 03), ni un mecanismo de
migración hacia la BD real — es exclusivamente el fixture reproducible
para levantar una réplica estructural desechable en CI/tests locales.
