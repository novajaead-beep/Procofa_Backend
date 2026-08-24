# PROCOFA — Auditoría Técnica del Baseline V2.1 y Mapa de Implementación

**Fecha:** 2026-08-23
**Fuentes analizadas:** `procofa_bdFinal.sql` (dump custom de `pg_dump`, PostgreSQL 18.3, base `procofa_audit_db`) vs `PROCOFA_CLAUDE_HANDOFF_V2_1.md`
**Método:** el dump no es SQL plano — es un archivo *custom format* de `pg_dump` (firma `PGDMP`). Se parseó con `pgdumplib` (Python) extrayendo los 522 objetos del TOC, y adicionalmente se **aplicó el DDL completo contra un PostgreSQL 16 real en un contenedor Linux** para verificar empíricamente portabilidad y consistencia (no solo lectura estática).

---

## 📌 Correcciones aprobadas (2026-08-23, turno 2) — decisiones definitivas

La auditoría fue revisada y **aprobada como base arquitectónica** con las siguientes 14 correcciones. Ya están incorporadas sección por sección en el cuerpo de este documento (reemplazan las recomendaciones equivalentes de la versión original) y **no se vuelven a cuestionar** salvo contradicción funcional nueva.

1. Testcontainers/CI usan **PostgreSQL 18**, no 16. (La verificación empírica de este documento se hizo contra PG16 por disponibilidad de herramientas en el entorno de auditoría — el DDL no usa sintaxis específica de versión, pero la imagen de referencia para CI/Testcontainers es `postgres:18`.)
2. No usar `ValueGeneratedNever()` de forma general en los PK UUID. El modelo EF declara `HasDefaultValueSql("gen_random_uuid()")` + `ValueGeneratedOnAdd()`, reflejando el default real de la BD; Domain puede seguir asignando GUIDs explícitos (EF los respeta si el valor no es el default de CLR).
3. `procofa_app` nunca recibe privilegios DDL — sigue siendo exclusivamente runtime (DML). El baseline y las migraciones se ejecutan con una credencial administrativa/de migración separada, nunca con `procofa_app`.
4. `ITenantUnitOfWork` no construye un `DbContext` sobre una conexión externa. Abre la transacción a través del **mismo** `ProcofaDbContext`, ejecuta `set_config(..., true)` a través de ese mismo `DbContext`, y corre las queries en el mismo scope/transacción.
5. Prohibido `enum → UUID` vía diccionario estático para catálogos. Los UUID son detalle de persistencia (varían por entorno/seed); la identidad semántica estable es `code`. Resolución en runtime por `code`, no valores hardcodeados.
6. La validación de consistencia intra-auditoría se generaliza más allá de Finding↔AuditCriterion: también `Client↔AuditedCompany↔CompanySite`, `AuditCriterion↔AuditChecklist↔Criterion`, y toda referencia de Evidence/Observation/Finding/CorrectiveAction — como mecanismo reusable de Application, no validaciones puntuales.
7. SHA-256 y validaciones esenciales de evidencia (tamaño, MIME real) se resuelven **de forma síncrona durante la carga**, antes de aceptar/persistir la evidencia. Outbox queda reservado para procesamiento posterior.
8. `RefreshToken` **no** escribe en `access_logs` (su `event_type` no admite ese valor — el CHECK solo permite `LOGIN_SUCCESS/LOGIN_FAILURE/LOGOUT/PASSWORD_RESET_REQUEST/PASSWORD_RESET_SUCCESS/ACCOUNT_LOCKED`). Por ahora usa structured logging; ampliar el CHECK es una migración futura explícita si se necesita auditoría específica de refresh.
9. `programs`, `profiles` y `audit_types` serán administrables por ADMIN a futuro (vía una migración de GRANTs DML controlada, todavía no aplicada). Los catálogos estructurales de workflow (`roles`, `permissions`, `role_permissions`, y los 5 catálogos de estado/compliance) permanecen controlados por despliegue, solo lectura para `procofa_app`. **No se cambian GRANTs todavía.**
10. `checklists.audit_type_id IS NULL` = checklist genérico para (Program, Profile). La resolución prioriza coincidencia exacta por `audit_type_id`; usa `NULL` como fallback.
11. Inmutabilidad de `checklist_versions PUBLISHED` se enforza en Application por ahora; defensa SQL (trigger análogo a `prevent_final_report_mutation`) se añade más adelante, antes de producción.
12. `validate_audit_before_close()` se mantiene como defensa PostgreSQL; `CloseAuditUseCase` replica la misma validación para dar error de dominio limpio.
13. `finding_number` nunca se asigna con `MAX+1` sin sincronización — serializar por auditoría o implementar reintento seguro ante violación UNIQUE.
14. Los scripts de funciones/triggers/RLS se versionan junto con el historial de cambios de persistencia — pueden vivir como `.sql`, pero cada cambio futuro debe quedar vinculado a la migración/release correspondiente (sin una segunda línea de evolución desconectada de EF migrations).

---

## A. Resumen de comprensión del sistema

1. PROCOFA digitaliza el ciclo PHVA de auditorías OEA/C-TPAT: Planeación → Ejecución → Hallazgos → Acciones correctivas → Cierre → Reporte, con generación Word/PDF.
2. Etapa 1 es de uso interno PROCOFA con un solo tenant lógico, pero la base física **ya está construida multitenant** (RLS real, no cosmético).
3. El baseline físico (48 tablas) **coincide al 100%, nombre por nombre**, con el inventario de 48 tablas listado en el handoff (sección 35). No hay tablas de más ni de menos.
4. Tenant ≠ Client está modelado correctamente: `tenants` es la organización dueña del sistema (1 fila, GUID fijo `00000000-0000-0000-0000-000000000001`, slug `procofa`); `clients` es quien contrata el servicio.
5. Los 5 roles del sistema están sembrados exactamente como se definieron, y **la matriz `role_permissions` (30 filas / 17 permisos) reproduce con precisión quirúrgica** las capacidades descritas en el handoff para cada rol (verificado permiso por permiso, ver sección D).
6. `audit_team.audit_role` (LEAD/SUPPORT) es independiente del rol de sistema, con un índice único parcial que garantiza un solo LEAD por auditoría.
7. `execution_mode` existe físicamente y la regla condicional de `company_site_id` **no está implementada en el esquema** (ni CHECK ni trigger) — exactamente como exige el handoff: la regla vive en Domain/Application.
8. El versionado de checklists (`checklists → checklist_versions → checklist_sections → criteria`) y el snapshot evaluable (`audit_criteria`, separado de `criteria`) están completos y correctamente enlazados vía `audit_checklists`.
9. La concurrencia optimista (`lock_version`) está presente en `audit_criteria`, `findings` y `corrective_actions` — como tres unidades de concurrencia **independientes**, no una sola en `audits`.
10. Idempotencia (`idempotency_operations`) y Outbox transaccional (`outbox_messages`) están completos y son más ricos de lo mínimo descrito (guardan `response_payload` para replay y `available_at_utc`/`attempts` para backoff).
11. `audit_logs` es append-only **por partida doble**: trigger que rechaza UPDATE/DELETE *y* el rol de runtime `procofa_app` ni siquiera tiene el privilegio GRANT de UPDATE/DELETE sobre esa tabla.
12. La inmutabilidad de reportes FINAL está implementada y **ya corregida** en la versión correcta que menciona el handoff (el `DELETE` retorna `OLD`, no `NEW`).
13. El patrón RLS físico (`FORCE ROW LEVEL SECURITY` + policy `tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid`) es **textualmente compatible** con el patrón `SET LOCAL app.tenant_id` + EF Core descrito en el handoff, y es *fail-closed*: sin `SET LOCAL`, cualquier query retorna 0 filas, nunca datos cruzados.
14. Hay un mecanismo de integridad **no documentado en el handoff pero totalmente compatible con él**: la función `enforce_same_tenant_references()`, aplicada en 30+ triggers, que valida en cada INSERT/UPDATE que las FK referenciadas pertenezcan al mismo tenant.
15. La base está "vacía" a propósito: todas las tablas transaccionales tienen 0 filas; solo los catálogos y el tenant están sembrados. Esto es un baseline limpio de arranque, no datos de prueba.

---

## B. Estado del baseline PostgreSQL

### Inventario cuantitativo (extraído del TOC del dump, 522 objetos)

| Componente | Cantidad |
|---|---|
| Tablas | 48 |
| FK constraints | 126 |
| PK + UNIQUE constraints | 73 (48 PK + 25 UNIQUE) |
| Índices explícitos adicionales (`CREATE INDEX`) | 36 (3 son únicos parciales) |
| Índices totales en el esquema (implícitos de PK/UNIQUE + explícitos) | 109 — verificado empíricamente |
| Triggers | 54 (89 entradas en `information_schema.triggers`, porque un trigger `BEFORE INSERT OR UPDATE` cuenta como 2 filas ahí — no es un error, es el comportamiento estándar de esa vista) |
| Funciones PL/pgSQL | 6 |
| Tablas con RLS (`FORCE ROW LEVEL SECURITY` + policy) | 36, todas con exactamente 1 policy `*_tenant_isolation` |
| Tablas de catálogo global sin RLS | 12 |
| Sentencias ACL (GRANT/REVOKE) | 50 |
| Extensiones | 2 (`pgcrypto`, `uuid-ossp`) |
| Secuencias, vistas, materialized views, domains, tipos custom, stored procedures | 0 — todo PK es UUID vía `gen_random_uuid()`, no hay lógica de negocio en funciones fuera de las 6 de trigger |
| Roles de PostgreSQL (`CREATE ROLE`) | 0 en el dump — **esperado**: `pg_dump` de una sola base no incluye roles de clúster; `procofa_owner`/`procofa_app` deben aprovisionarse aparte (confirma sección 51 del handoff) |

### Las 6 funciones PL/pgSQL (inventario completo, no solo mención)

| Función | Propósito | Se dispara en |
|---|---|---|
| `enforce_same_tenant_references()` | Valida que cada FK referenciada pertenezca al mismo `tenant_id` que la fila que se inserta/actualiza | 30 triggers, uno por tabla tenant-scoped con FKs salientes |
| `normalize_user_email()` | `email = BTRIM(email)`, `normalized_email = UPPER(BTRIM(email))` | `users`, BEFORE INSERT OR UPDATE OF email |
| `prevent_audit_log_mutation()` | RAISE EXCEPTION incondicional | `audit_logs`, BEFORE UPDATE y BEFORE DELETE (2 triggers) |
| `prevent_final_report_mutation()` | Bloquea UPDATE/DELETE si `OLD.status = 'FINAL'`; maneja correctamente `RETURN OLD` en DELETE | `audit_reports`, BEFORE UPDATE OR DELETE |
| `set_updated_at_utc()` | `NEW.updated_at_utc = NOW()` | 13 tablas con `updated_at_utc`, BEFORE UPDATE |
| `validate_audit_before_close()` | Si el nuevo `status_id` resuelve a `code = 'CERRADA'`: exige que no existan `audit_criteria` obligatorios sin evaluar y que `validated_by_user_id`/`validated_at_utc` no sean NULL; autocompleta `closed_at_utc` | `audits`, BEFORE INSERT OR UPDATE OF status_id |

### Las 48 tablas (verificación de inventario completo, agrupadas como en el handoff)

Identidad/seguridad (9): `tenants, roles, permissions, role_permissions, users, user_roles, password_reset_tokens, refresh_tokens, access_logs`
Clientes (6): `clients, client_programs, audited_companies, company_sites, client_contacts, user_client_access`
Catálogos (9): `programs, profiles, audit_types, audit_statuses, compliance_statuses, finding_types, finding_priorities, finding_statuses, corrective_action_statuses`
Checklists (4): `checklists, checklist_versions, checklist_sections, criteria`
Auditorías (7): `audits, audit_programs, audit_checklists, audit_team, audit_criteria, observations, audit_document_requests`
Evidencias/hallazgos (4): `audit_evidences, findings, corrective_actions, finding_followups`
Reportes (5): `audit_results, report_templates, report_template_versions, audit_reports, audit_signatories`
Infraestructura (4): `notifications, idempotency_operations, outbox_messages, audit_logs`

**Total: 48/48. Coincidencia exacta con el handoff, sin excepciones.**

### Seeds (datos reales encontrados, no solo estructura)

- `tenants`: 1 fila — `id = 00000000-0000-0000-0000-000000000001`, `slug = 'procofa'`.
- `roles`: 5 — ADMIN, AUDITOR_LIDER, AUDITOR_APOYO, CLIENTE, CONSULTOR (con las mismas descripciones funcionales del handoff, palabra por palabra).
- `permissions`: 17. `role_permissions`: 30 asignaciones.
- `programs`: 2 (OEA, CTPAT). `profiles`: 6. `audit_types`: 6. `audit_statuses`: 7 (BORRADOR, PROGRAMADA, EN_PROCESO, REVISION, SEGUIMIENTO, CERRADA*, CANCELADA* — *terminales). `compliance_statuses`: 4 (CUMPLE=100, CUMPLE_PARCIAL=50, NO_CUMPLE=0, NO_APLICA=excluido del score). `finding_types`: 3. `finding_priorities`: 3. `finding_statuses`: 5. `corrective_action_statuses`: 6.
- Todas las tablas transaccionales (`users`, `clients`, `audits`, `findings`, etc.): **0 filas**. No hay ni un usuario todavía — el primer paso operativo real será sembrar un ADMIN.

---

## C. Inconsistencias encontradas

| Severidad | Componente | Problema | Riesgo | Recomendación |
|---|---|---|---|---|
| ✅ Resuelta | Integridad referencial cruzada (generalizada) | No existe constraint/trigger que valide consistencia intra-auditoría en `findings`/`observations`/`audit_evidences` (mismo patrón potencial en `Client↔AuditedCompany↔CompanySite` y `AuditCriterion↔AuditChecklist↔Criterion`). | La BD permite físicamente estados que el handoff prohíbe (sección 39/57). No es problema de tenant (ya cubierto por `enforce_same_tenant_references`), es de consistencia intra-tenant/intra-auditoría. | **Decisión definitiva (turno 2 — alcance ampliado):** no es solo Finding↔AuditCriterion — se generaliza a un patrón reusable de Application que cubre también `Client↔AuditedCompany↔CompanySite`, `AuditCriterion↔AuditChecklist↔Criterion`, y toda referencia de Evidence/Observation/Finding/CorrectiveAction (detalle en sección F, subsección "Validación de consistencia intra-auditoría"). |
| ✅ Resuelta | Grants de `procofa_app` sobre catálogos | 12 tablas de catálogo global tenían **GRANT SELECT únicamente** para `procofa_app`, ambiguo contra el permiso `CATALOGS_MANAGE`. | — | **Decisión definitiva (turno 2):** `programs`, `profiles` y `audit_types` serán administrables por ADMIN a futuro vía una migración de GRANTs DML controlada (no aplicada todavía). Los 9 catálogos restantes (`roles`, `permissions`, `role_permissions`, `audit_statuses`, `compliance_statuses`, `finding_types`, `finding_priorities`, `finding_statuses`, `corrective_action_statuses`) permanecen de solo lectura para `procofa_app`, controlados por despliegue — **no se cambian GRANTs todavía**. `procofa_app` nunca recibe privilegios DDL en ningún caso. |
| 🟠 Alta | Portabilidad del `CREATE DATABASE` del dump | El header del dump crea la BD con `LOCALE = 'Spanish_Mexico.1252'` (nomenclatura Windows). **Verificado empíricamente** (con PostgreSQL 16 Linux, único cliente/servidor disponible en el entorno de esta auditoría — el hallazgo es sobre el nombre del locale, no específico de versión): `CREATE DATABASE ... LOCALE = 'Spanish_Mexico.1252'` falla con `ERROR: invalid LC_COLLATE locale name "Spanish_Mexico.1252"`. | Bloquea recrear la BD tal cual en un contenedor Linux. **Decisión definitiva (turno 2):** Testcontainers/CI usan imagen `postgres:18` (no 16) — pendiente re-confirmar el mismo resultado del locale específicamente sobre 18 cuando se arme el pipeline de CI, aunque no hay razón para esperar un comportamiento distinto (el parseo de `LC_COLLATE` no cambió entre 16 y 18). | **No usar el `CREATE DATABASE` del dump en Linux, en ninguna versión.** Crear la BD de prueba/CI con `postgres:18` + locale estándar del contenedor (`en_US.UTF-8`/`C.UTF-8`) y aplicar el resto del DDL (tablas/constraints/triggers/RLS), que **se verificó que aplica limpio, sin un solo error**. Si el ordenamiento en español importa para la UI, usar `LOCALE_PROVIDER = icu, ICU_LOCALE = 'es-MX'` consistente en todos los entornos. |
| ✅ Resuelta | Trigger `validate_audit_before_close()` | La BD ya enforza en un trigger la misma regla que el handoff (sección 38) asigna a `CloseAuditUseCase`. | — | **Decisión definitiva (turno 2):** se mantiene como defensa PostgreSQL (última línea ante bypass del Application layer); `CloseAuditUseCase` replica exactamente las mismas condiciones para devolver un error de dominio limpio (409/422) en vez de dejar que el usuario vea la excepción SQL cruda. |
| 🟡 Media | Semántica de estados no documentada | Los 7 valores de `audit_statuses` y los 6 de `corrective_action_statuses` existen físicamente (con `sort_order`/`is_terminal`/`is_closed`) pero **no están enumerados en ningún lugar del handoff** (a diferencia de `finding_statuses` y `compliance_statuses`, que sí lo están). Tampoco existe en la BD ni en el handoff un grafo de transiciones válidas (ej. ¿se puede pasar de `PROGRAMADA` a `CERRADA` directo?). | Ambigüedad de implementación al construir la máquina de estados de `Audit` y `CorrectiveAction` en Domain. | Definir explícitamente el grafo de transiciones antes de implementar el módulo de Ejecución (Fase 5), no antes de Fase 1. No bloquea Foundation. |
| ✅ Resuelta | Selector de checklist más granular de lo documentado | `ix_checklists_selector` indexa también `audit_type_id` (nullable), no solo programa+perfil. | — | **Decisión definitiva (turno 2):** `audit_type_id IS NULL` = checklist genérico para (Program, Profile). La resolución en `CreateAudit` prioriza coincidencia exacta por `audit_type_id`; si no hay match exacto, usa la versión con `audit_type_id IS NULL` como fallback. |
| ✅ Resuelta | Asimetría de inmutabilidad | Reportes `FINAL` tienen trigger de protección; `checklist_versions PUBLISHED` no tenía mecanismo equivalente. | — | **Decisión definitiva (turno 2):** se enforza en Application por ahora (bloquear UPDATE de `checklist_sections`/`criteria` si la versión padre no está en `DRAFT`). Defensa SQL simétrica (`prevent_published_checklist_mutation()`, análoga a la de reportes) se añade más adelante, antes de producción — no ahora. |
| 🟢 Baja | Falta de `ALTER DEFAULT PRIVILEGES` | Los 50 GRANTs del dump son explícitos por objeto; no hay default privileges para tablas futuras. | Mitigado por el flujo de migración con credencial administrativa (**decisión definitiva, punto 3**): como el baseline y las migraciones corren con una credencial distinta a `procofa_app` (que nunca tiene privilegios DDL), el riesgo real es solo que esa migración administrativa olvide el GRANT DML explícito para `procofa_app` sobre la tabla nueva — un olvido de proceso, no un problema de arquitectura de privilegios. | Incluir el GRANT explícito como parte del checklist de toda migración futura que cree una tabla nueva, ejecutado por la credencial administrativa. |
| 🟢 Baja | `password_reset_tokens.token_hash` sin UNIQUE | `refresh_tokens.token_hash` tiene `UNIQUE`; `password_reset_tokens.token_hash` no. | Muy bajo — colisión estadísticamente insignificante con un hash criptográfico de buena entropía. | Opcional: agregar `UNIQUE` por consistencia con el patrón usado en `refresh_tokens`. |

**No se encontraron inconsistencias en:** el modelo de tenancy, la cardinalidad de roles, la separación `criteria`/`audit_criteria`, la obligatoriedad de `audit_criterion_id` en `findings`, la presencia y tipo de los tres `lock_version`, la estructura de `idempotency_operations`/`outbox_messages`, el carácter append-only de `audit_logs`, ni el patrón SQL de las RLS policies — todos coinciden exactamente con lo especificado (detalle en sección D).

---

## D. Decisiones congeladas que confirmo

- **Tenancy:** `tenants` (1 fila, GUID fijo) ≠ `clients` (0 filas, tabla propia tenant-scoped). Confirmado en columnas y FKs.
- **Roles — exactamente 5:** `ADMIN, AUDITOR_LIDER, AUDITOR_APOYO, CLIENTE, CONSULTOR`, sembrados en `roles` con esas descripciones exactas. Verifiqué además que **la matriz `role_permissions` es funcionalmente coherente rol por rol**: ADMIN (6 permisos: gestión de usuarios/clientes/catálogos + solo lectura de auditorías/reportes/bitácora — nunca evalúa ni crea hallazgos), AUDITOR_LIDER (12 permisos: el único con `AUDITS_CREATE`, `AUDITS_ASSIGN_TEAM`, `FINDINGS_VALIDATE`, `CORRECTIVE_ACTION_VALIDATE`, `REPORTS_VALIDATE`), AUDITOR_APOYO (6 permisos: evalúa y propone pero no valida ni crea auditorías), CLIENTE (4 permisos: lectura + `CORRECTIVE_ACTION_RESPOND` + `EVIDENCE_UPLOAD`), CONSULTOR (2 permisos: solo `AUDITS_READ`+`REPORTS_READ`).
- **System Role ≠ audit_role:** `audit_team.audit_role` restringido por CHECK a `LEAD`/`SUPPORT`, independiente de `roles`. Un índice único parcial (`uq_audit_team_one_lead ... WHERE audit_role='LEAD'`) garantiza un solo LEAD por auditoría.
- **Acceso cliente vía `user_client_access`:** tabla presente, tenant-scoped, PK compuesta `(user_id, client_id)`. Tener rol CLIENTE no otorga acceso implícito a ningún cliente.
- **Jerarquía Client → AuditedCompany → CompanySite:** confirmada por FKs (`audited_companies.client_id`, `company_sites.audited_company_id`), con `client_id` y `audited_company_id` como columnas independientes en `audits`.
- **`execution_mode` (ONSITE/REMOTE/HYBRID):** columna real con CHECK de enum; `company_site_id` es nullable a nivel de columna; **no existe** ningún CHECK ni trigger que fuerce la obligatoriedad condicional — la regla vive donde el handoff exige que viva: Domain/Application.
- **Checklists versionados:** `checklists → checklist_versions → checklist_sections → criteria`, con `audit_checklists` fijando la versión usada por cada auditoría (`FK RESTRICT` en toda la cadena, protegiendo el histórico de borrado).
- **Criterion vs AuditCriterion:** `criteria` (plantilla) y `audit_criteria` (snapshot con `criterion_code_snapshot`, `question_snapshot`, `normative_reference_snapshot`, `is_mandatory_snapshot`) son tablas físicamente distintas, tal como exige la sección 9.
- **Hallazgos ligados a `AuditCriterion`:** `findings.audit_criterion_id` es `NOT NULL` con FK a `audit_criteria`.
- **Concurrencia:** `audit_criteria.lock_version`, `findings.lock_version`, `corrective_actions.lock_version` — los tres `bigint DEFAULT 1 NOT NULL` con `CHECK (lock_version > 0)`. Ninguno tiene incremento automático por trigger: el incremento es responsabilidad de Application/EF Core (`IsConcurrencyToken()` + incremento explícito antes de `SaveChanges`).
- **Idempotencia:** `idempotency_operations` con `UNIQUE (tenant_id, operation_id)`, y además guarda `response_payload`/`response_status_code` para replay — más completo de lo mínimo descrito.
- **Outbox:** `outbox_messages` con `status`, `attempts`, `available_at_utc`, índice parcial para el worker (`WHERE status IN ('PENDING','FAILED')`).
- **Bitácora append-only:** `audit_logs` — trigger que rechaza UPDATE/DELETE **y** el GRANT de `procofa_app` sobre esa tabla es solo `SELECT, INSERT` (doble enforcement).
- **Reportes:** `audit_results` (1:1 con `audits` vía `UNIQUE(audit_id)`), `report_templates → report_template_versions`, `audit_reports` (versionado, `status` DRAFT/FINAL/VOID), `audit_signatories`. La función `prevent_final_report_mutation()` **ya tiene la versión correcta** (maneja `DELETE` retornando `OLD`), no la versión con el bug que el handoff pide explícitamente no reintroducir.
- **RLS:** las 36 policies usan literalmente `tenant_id = (NULLIF(current_setting('app.tenant_id', true), '')::uuid)` en `USING` y `WITH CHECK`, sobre tablas con `FORCE ROW LEVEL SECURITY`. Es 100% compatible con el patrón `BEGIN; SELECT set_config('app.tenant_id', @tenantId, true); ... COMMIT;` de `ITenantContext`/`ITenantUnitOfWork`, incluyendo lecturas.

---

## E. Mapa de proyectos .NET

```
Procofa.sln
src/
├── Procofa.Domain            (sin dependencias externas)
├── Procofa.Application       (→ Domain)
├── Procofa.Infrastructure    (→ Application, → Domain)
└── Procofa.Api                (→ Application; → Infrastructure SOLO en el composition root/Program.cs)
tests/
├── Procofa.Domain.Tests       (→ Domain)
├── Procofa.Application.Tests  (→ Application, mocks de puertos)
├── Procofa.IntegrationTests   (→ Infrastructure, Testcontainers PostgreSQL real)
└── Procofa.Api.Tests          (→ Api, WebApplicationFactory + Testcontainers)
```

Reglas de dependencia (ya fijadas en el handoff, confirmo que se respetan sin necesidad de proyectos adicionales):

- `Domain` no referencia EF Core, Npgsql, ASP.NET, ni ningún paquete de infraestructura.
- `Application` define puertos (`IEvidenceStorage`, `IReportGenerator`, `ITenantContext`, `ITenantUnitOfWork`, repositorios como interfaces) y casos de uso; solo depende de `Domain`.
- `Infrastructure` implementa los puertos: `Persistence` (EF Core + `ProcofaDbContext` + Fluent Configurations + Migrations), `Tenancy` (implementación de `ITenantUnitOfWork`), `Storage` (S3-compatible), `Reports` (OpenXML/PDF), `Outbox` (background worker), `Authentication` (JWT, `PasswordHasher`), `BackgroundJobs`, `Notifications`.
- `Api` contiene Controllers/Endpoints, Middleware, Authorization handlers, Hubs (SignalR), y el `Program.cs` como único lugar que conoce tanto `Application` como `Infrastructure` (DI registration).

No se proponen capas adicionales (`Manager`, `Service` genérico, `GenericRepository`) — cada aggregate root tendrá su propio puerto de persistencia específico cuando lo necesite, no un repositorio genérico universal.

---

## F. Agregados y modelo de dominio propuesto

**Metodología:** no mapeo tabla→aggregate 1:1. Uso la semántica real de las FKs como evidencia: `ON DELETE CASCADE` sugiere una entidad *poseída* dentro del aggregate del padre; `ON DELETE RESTRICT`/`SET NULL` sugiere una *referencia* entre aggregates independientes. La señal más fuerte es la presencia de `lock_version` propio: una tabla con su propio token de concurrencia está diseñada para actualizarse **sin contender** con su "padre" aparente — eso la convierte en su propio aggregate root, no en una entidad interna.

### Client
```
Client (Aggregate Root)
├── responsabilidad: identidad fiscal/comercial del cliente que contrata el servicio
├── entidades internas: ClientContact (FK client_id ON DELETE CASCADE → poseída)
├── value objects: ClientProgram[] (junction sin atributos propios)
├── invariantes: tax_id único por tenant (si se provee); no se puede desactivar con auditorías abiertas (regla de Application, no de BD)
└── límite transaccional: alta/edición de cliente + sus contactos + programas asociados
```

### AuditedCompany (aggregate propio, NO dentro de Client)
```
AuditedCompany (Aggregate Root) — referencia ClientId
├── responsabilidad: la organización física que se audita (que puede o no ser el propio Client)
├── entidades internas: CompanySite (FK audited_company_id ON DELETE CASCADE → poseída)
├── invariantes: tax_id único por (tenant, client); geolocalización de sitios
└── límite transaccional: alta de empresa auditada + sus sitios
```
*Evidencia de separación de Client:* `audited_companies.client_id` es `ON DELETE RESTRICT` (no CASCADE) — el diseño físico protege a `AuditedCompany` de desaparecer si se toca `Client`, señal de aggregates independientes con referencia por ID, no de composición.

### Checklist / ChecklistVersion (dos aggregate roots, no uno)
```
Checklist (Aggregate Root)
├── responsabilidad: encabezado/familia (program_id, profile_id, audit_type_id?, nombre)
└── límite transaccional: metadatos del encabezado únicamente

ChecklistVersion (Aggregate Root) — referencia ChecklistId
├── responsabilidad: contenido evaluable de una versión concreta
├── entidades internas: ChecklistSection → Criterion (poseídas, solo editables mientras status = DRAFT)
├── invariantes: version_number único por checklist; una vez PUBLISHED, secciones/criterios inmutables (hoy solo enforzado en Application — ver hallazgo 🟢 en sección C)
└── límite transaccional: publicar una versión completa (secciones + criterios) de una vez
```

### Audit
```
Audit (Aggregate Root)
├── responsabilidad: la instancia de auditoría — planeación, ejecución, cierre
├── entidades internas: AuditTeam (CASCADE, sin lock_version propio), AuditProgram[] (VO), AuditChecklist (referencia congelada a la versión usada), AuditDocumentRequest (CASCADE, sin lock_version), AuditResult (1:1, CASCADE, sin lock_version — se finaliza junto con el cierre), AuditSignatory (CASCADE directo desde audits, no desde audit_reports)
├── invariantes: execution_mode↔company_site_id (Domain), un solo LEAD en equipo (espeja el índice único parcial de BD), no cerrar con criterios obligatorios sin evaluar (espeja el trigger validate_audit_before_close)
└── límite transaccional: creación de auditoría + programas + checklist + equipo inicial; cierre de auditoría + resultado + cambio de estado
```
*Nota importante:* `AuditSignatory` tiene `audit_id` pero **no** `report_id` — pertenece al Audit, no al Report, a diferencia de lo que podría asumirse.

### AuditCriterion (aggregate root propio, separado de Audit)
```
AuditCriterion (Aggregate Root) — referencia AuditId + ChecklistVersionId(vía AuditChecklist) + CriterionId
├── responsabilidad: la evaluación concreta de un criterio dentro de una auditoría (snapshot + respuesta)
├── entidades internas: Observation[] (FK audit_criterion_id ON DELETE CASCADE, sin lock_version — historial de comentarios)
├── invariantes: lock_version propio (concurrencia optimista independiente); snapshot inmutable una vez creado
└── límite transaccional: autosave de una evaluación individual — debe poder guardarse SIN bloquear el resto de la auditoría
```
*Evidencia:* `lock_version` propio existe precisamente para que dos evaluadores editando criterios distintos de la misma auditoría no contiendan entre sí. Anidarlo dentro del aggregate `Audit` obligaría a cargar/bloquear toda la auditoría en cada autosave — contradice el propósito del token.

### Finding
```
Finding (Aggregate Root) — referencia AuditId + AuditCriterionId
├── responsabilidad: no conformidad/observación/oportunidad de mejora detectada
├── entidades internas: FindingFollowup[] (CASCADE desde finding_id, sin lock_version — bitácora de seguimiento, puede taggear opcionalmente un corrective_action_id sin pertenecerle)
├── invariantes: audit_criterion_id obligatorio; audit_criterion.audit_id debe coincidir con finding.audit_id (enforzado en Application vía el patrón general de validación intra-auditoría — ver subsección debajo de los aggregates); lock_version propio
└── límite transaccional: creación/validación de un hallazgo + su primer followup si aplica
```

### CorrectiveAction (aggregate root propio, separado de Finding)
```
CorrectiveAction (Aggregate Root) — referencia FindingId
├── responsabilidad: la acción correctiva y su ciclo de vida (responder, validar, cerrar)
├── invariantes: responsable (user o contacto) obligatorio; lock_version propio
└── límite transaccional: respuesta del cliente / validación del auditor, cada una atómica e independiente del resto del Finding
```
*Evidencia de separación:* `corrective_actions` tiene su propio `lock_version` — un CLIENTE respondiendo una acción correctiva no debe contender con un AUDITOR_LIDER validando otro aspecto del mismo Finding.

### Report (AuditReport como aggregate root)
```
AuditReport (Aggregate Root) — referencia AuditId + ReportTemplateVersionId
├── responsabilidad: el documento generado (versión, formato, estado)
├── invariantes: FINAL es inmutable (trigger prevent_final_report_mutation ya presente y correcto); (audit_id, report_type, version_number, format) único
└── límite transaccional: generación de una versión de reporte; validación/finalización
```
`ReportTemplate`/`ReportTemplateVersion` siguen el mismo patrón dual que Checklist/ChecklistVersion (RESTRICT en cascada, versionado, protección del histórico).

### Validación de consistencia intra-auditoría (patrón general)

**Decisión aprobada (turno 2):** la validación de "no cruzar recursos entre auditorías distintas del mismo tenant" no es un caso puntual de Finding — es un patrón reusable que debe aplicarse consistentemente en Application a **toda** relación padre-hijo que involucre una auditoría, ya que la BD solo garantiza same-tenant (`enforce_same_tenant_references`), no same-audit:

- `Finding.AuditCriterionId` → el `AuditCriterion` referenciado debe pertenecer a `Finding.AuditId`.
- `Observation.AuditCriterionId` → mismo chequeo contra `Observation.AuditId`.
- `AuditEvidence` → si trae `AuditCriterionId`/`FindingId`/`CorrectiveActionId`, cada uno debe resolver transitivamente al mismo `AuditId` que `AuditEvidence.AuditId`.
- `AuditCriterion.CriterionId` → el `Criterion` debe pertenecer a la `ChecklistVersion` que `AuditCriterion.AuditChecklistId` (vía `AuditChecklist`) tiene congelada para esa auditoría — no a cualquier versión del checklist.
- `Client → AuditedCompany → CompanySite` → al crear/editar un `Audit`, `AuditedCompanyId` debe pertenecer a `ClientId`, y si `CompanySiteId` está presente, debe pertenecer a `AuditedCompanyId` — la misma clase de chequeo transitivo, aplicada a la jerarquía comercial en vez de a la ejecución.

Implementación recomendada: un helper/guard de Application (ej. `IAuditScopeGuard` o un conjunto de extension methods sobre los comandos relevantes) invocado por cada comando que persiste una entidad con estas referencias — centraliza la lógica en un solo lugar en vez de reimplementarla ad hoc en cada use case.

### Enums de dominio vs catálogos persistentes

La BD distingue dos estilos de "valores de estado", y la elección de modelado en C# debe seguir la misma distinción:

- **Backed por VARCHAR+CHECK** (sin tabla propia): `execution_mode`, `audit_reports.status`, `checklist_versions.status`, `report_template_versions.status`, `audit_document_requests.status`, `idempotency_operations.status`, `outbox_messages.status`, `audit_team.audit_role`, `audit_signatories.signer_type`, `audit_evidences.evidence_type`. → **Enums de C#** con `HasConversion<string>()` explícito (nunca depender de `Enum.ToString()` por defecto, tal como advierte la sección 33 del handoff).
- **Backed por tabla de catálogo con FK** (`roles, permissions, programs, profiles, audit_types, audit_statuses, compliance_statuses, finding_types, finding_priorities, finding_statuses, corrective_action_statuses`): tienen `sort_order`/`is_terminal`/`is_closed`/`score_weight`. → **Decisión definitiva:** nunca un `enum → UUID` vía diccionario estático — el UUID es un detalle de persistencia que puede variar por entorno/reseed; la identidad semántica estable es `code`. Modelarlos como **entidades de referencia ligeras** cacheadas en memoria (ej. un `ICatalogLookup<TCatalog>` resuelto por proceso, con invalidación corta), indexadas por `code`; Domain/Application comparan y razonan por `code` (`"ADMIN"`, `"OEA"`, `"CUMPLE"`), nunca por un GUID escrito en el código fuente. `programs`/`profiles`/`audit_types` seguirán este patrón siendo además editables por ADMIN a futuro (permiso `CATALOGS_MANAGE` ampliado vía migración de GRANTs DML, todavía no aplicada); los 9 catálogos restantes (roles, permissions, role_permissions, y los 5 de estado/compliance) usan el mismo patrón de lookup por `code` pero permanecen de solo lectura para `procofa_app` — controlados por despliegue.

### Domain Events (uso acotado, no ceremonial)

Consistente con la sección 20 del handoff ("no es event sourcing"), los domain events aquí son solo el mecanismo interno para disparar `audit_logs` + `outbox_messages` desde el mismo `SaveChanges`, no para reconstruir estado: `AuditClosed`, `FindingCreated`, `FindingValidated`, `CorrectiveActionValidated`, `ReportFinalized`, `ChecklistVersionPublished`. Deliberadamente **no** incluyo un evento por cada autosave de criterio (`AuditCriterionEvaluated`) — es una operación de alta frecuencia mejor resuelta con un insert directo a `outbox_messages` cuando aplique, sin la ceremonia de un evento de dominio completo.

---

## G. Mapa de casos de uso

Clasificación: **C**ommand/**Q**uery · transacción · permiso (código de `permissions`) · tenant-scoped · bitácora (`audit_logs`) · outbox · concurrencia (409).

### Auth
| Caso de uso | Tipo | Tenant | Bitácora | Outbox | Concurrencia |
|---|---|---|---|---|---|
| Login | C | Sí (resuelto por config, no por JWT — aún no existe) | access_logs (`LOGIN_SUCCESS`/`LOGIN_FAILURE`) | No | No |
| RefreshToken | C | Sí | **No** — `access_logs.event_type` no tiene un valor válido para esto (CHECK cerrado a 6 valores); structured logging por ahora, CHECK se amplía en migración futura si se necesita | No | No |
| RequestPasswordReset / ResetPassword | C | Sí | access_logs (`PASSWORD_RESET_REQUEST`/`PASSWORD_RESET_SUCCESS`) | Notificación (email) | No |
| ChangePassword (must_change_password) | C | Sí | **No** — mismo motivo que RefreshToken, sin `event_type` correspondiente; structured logging | No | No |

### Clients (permiso `CLIENTS_MANAGE`)
| Caso de uso | Tipo | Tenant | Bitácora | Concurrencia |
|---|---|---|---|---|
| CreateClient / UpdateClient | C | Sí | Sí | No |
| CreateAuditedCompany / CreateCompanySite | C | Sí | Sí | No |
| CreateClientContact / GrantClientAccess / RevokeClientAccess | C | Sí | Sí | No |

### Checklists (permiso `CATALOGS_MANAGE`)
| Caso de uso | Tipo | Tenant | Bitácora | Concurrencia |
|---|---|---|---|---|
| CreateChecklist / CreateChecklistVersion (draft) | C | Sí | Sí | No |
| AddSection / AddCriterion | C | Sí | Sí | No |
| PublishChecklistVersion | C | Sí | Sí | No (transición de estado, no lock_version) |

### Audits (planeación)
| Caso de uso | Tipo | Permiso | Tenant | Bitácora | Concurrencia |
|---|---|---|---|---|---|
| CreateAudit | C | `AUDITS_CREATE` | Sí | Sí | No |
| AssignAuditTeam | C | `AUDITS_ASSIGN_TEAM` | Sí | Sí | No (espeja UNIQUE parcial de 1 LEAD) |
| GetAudit / ListAudits | Q | `AUDITS_READ` | Sí | No | — |
| UpdateAuditAssigned | C | `AUDITS_EDIT_ASSIGNED` | Sí | Sí | No |

### Audit Execution
| Caso de uso | Tipo | Permiso | Tenant | Bitácora | Idempotencia | Concurrencia |
|---|---|---|---|---|---|---|
| EvaluateCriterion (autosave) | C | `CRITERIA_EVALUATE` | Sí | Sí (por evento relevante, no cada autosave) | **Sí, obligatorio** | **Sí, 409** |
| AddObservation | C | `CRITERIA_EVALUATE` | Sí | Sí | No | No |
| GetAuditProgress | Q | `AUDITS_READ` | Sí | No | — | — |

### Evidence
| Caso de uso | Tipo | Permiso | Tenant | Bitácora | Outbox |
|---|---|---|---|---|---|
| UploadEvidence | C | `EVIDENCE_UPLOAD` | Sí | Sí | **No** para SHA-256/validación de tamaño/MIME real — eso ocurre síncronamente en el propio request, antes de aceptar/persistir la evidencia; Outbox solo para procesamiento posterior (notificación, indexado, etc.) |
| GetEvidenceDownloadUrl | Q | `AUDITS_READ` + autorización de recurso | Sí (metadata) / No (el stream sale de la transacción) | No | No |
| RequestDocument / RespondDocumentRequest | C | `AUDITS_CREATE`/cliente autorizado | Sí | Sí | No |

### Findings
| Caso de uso | Tipo | Permiso | Tenant | Bitácora | Concurrencia |
|---|---|---|---|---|---|
| CreateFinding | C | `FINDINGS_CREATE` | Sí | Sí | No (alta) |
| ValidateFinding / RejectFinding | C | `FINDINGS_VALIDATE` | Sí | Sí | **Sí, 409** |

### Corrective Actions
| Caso de uso | Tipo | Permiso | Tenant | Bitácora | Concurrencia |
|---|---|---|---|---|---|
| RespondCorrectiveAction | C | `CORRECTIVE_ACTION_RESPOND` | Sí | Sí | **Sí, 409** |
| ValidateCorrectiveAction | C | `CORRECTIVE_ACTION_VALIDATE` | Sí | Sí | **Sí, 409** |

### Reports
| Caso de uso | Tipo | Permiso | Tenant | Bitácora | Outbox |
|---|---|---|---|---|---|
| GenerateReport | C | `REPORTS_GENERATE` | Sí | Sí | **Sí** (job pesado en background) |
| ValidateReport / FinalizeReport (→ FINAL) | C | `REPORTS_VALIDATE` | Sí | Sí | No |
| GetReports / DownloadReport | Q | `REPORTS_READ` | Sí | No | No |

### Administration
| Caso de uso | Tipo | Permiso | Tenant | Bitácora |
|---|---|---|---|---|
| ViewAuditLog | Q | `AUDIT_LOG_READ` | Sí | — (es la propia bitácora, solo lectura) |

---

## H. Estrategia EF Core para la BD existente

**Principio:** el modelo EF debe *describir* el esquema que ya existe, no *crearlo*. La migración inicial debe quedar vacía en su cuerpo pero registrada en el historial.

### Pasos concretos

1. **No usar `Scaffold-DbContext` como fuente final.** Escribir a mano las entidades (POCOs puros en `Domain`, sin atributos de EF) y las `IEntityTypeConfiguration<T>` en `Infrastructure.Persistence.Configurations`, una por aggregate root/entidad, siguiendo los límites de la sección F. Scaffolding produce un modelo anémico tabla-por-tabla que no respeta los aggregates ya razonados.
2. **Configurar explícitamente lo que EF no puede inferir del esquema real:**
   - PK UUID: **corrección aprobada** — no `.ValueGeneratedNever()` general. Configurar `.HasDefaultValueSql("gen_random_uuid()")` + `.ValueGeneratedOnAdd()`, reflejando fielmente el default real de la columna. Domain sigue asignando GUIDs explícitos en el constructor del aggregate (necesario para emitir domain events/outbox con el ID conocido antes de `SaveChanges`); con `ValueGeneratedOnAdd()`, EF Core usa el valor asignado por el cliente siempre que no sea el default de CLR (`Guid.Empty`), y solo delegaría en el default de BD si la propiedad quedara sin establecer — consistente con lo que la BD realmente hace y útil para inserts fuera de la app (seeds, scripts).
   - `lock_version`: `.IsConcurrencyToken()` sobre la propiedad mapeada; el incremento es responsabilidad de la app (no hay trigger que lo haga) — vía un `SaveChanges` interceptor que incremente cualquier entidad rastreada en estado `Modified` que implemente una interfaz marcador (`IHasConcurrencyToken`), para no depender de que cada desarrollador lo recuerde manualmente.
   - Enums VARCHAR+CHECK: `.HasConversion<string>()` explícito, con el valor string exacto que espera el CHECK (ej. `"ONSITE"`, no `"Onsite"`).
   - PKs compuestas sin columna `id` (`audit_programs`, `audit_team`, `client_programs`, `role_permissions`, `user_client_access`, `user_roles`): `.HasKey(x => new { x.AuditId, x.ProgramId })`, mapeadas como colecciones/VOs dentro del aggregate dueño, sin `DbSet` propio.
   - `normalized_email`: la BD la recalcula siempre vía trigger (`UPPER(BTRIM(email))`) — no depender de que EF la lea de vuelta tras el insert; si Domain necesita el valor inmediatamente, replicar la misma lógica en C# con `CultureInfo.InvariantCulture` (evita el problema de "Turkish I") en vez de re-consultar.
   - Réplicar como `.HasCheckConstraint(...)` los CHECK relevantes en la configuración Fluent, para que futuras migraciones no los desconozcan.
3. **Migración baseline sin reconstrucción:**
   - Ejecutar `dotnet ef migrations add InitialBaseline` normalmente — EF generará un `Up()` que intenta crear las 48 tablas. Esto es esperado.
   - **Revisar el SQL generado** (`dotnet ef migrations script`) comparándolo objeto por objeto contra el dump: cualquier diferencia inesperada (un tipo, una longitud de `varchar`, un índice faltante) es señal de que el modelo EF todavía no coincide con la realidad — corregir el modelo, no el SQL generado, hasta que la migración sea trivial.
   - Vaciar el cuerpo de `Up()`/`Down()` de `InitialBaseline` (dejarlos sin sentencias — no `CREATE TABLE IF NOT EXISTS`, vacío puro).
   - Ejecutar `dotnet ef database update` normalmente contra la BD real: como el `Up()` está vacío, es 100% seguro — solo inserta la fila en `__EFMigrationsHistory`.
   - Verificar con `dotnet ef migrations list` y con una query de humo (`SELECT` sobre `roles`/`permissions` vía el `DbContext`) que el mapeo es correcto extremo a extremo.
   - A partir de aquí, toda migración nueva sigue el flujo normal: `migrations add` → revisar script → backup → aplicar controladamente (sección 50 del handoff).
4. **Lo que EF Core no gestiona y debe versionarse aparte:** triggers, funciones PL/pgSQL y RLS policies **no tienen representación nativa en el modelo de migraciones de EF Core**. Mantener el DDL de las 6 funciones + 54 triggers + 36 policies como scripts `.sql` en `Infrastructure/Persistence/Sql/` (fuente de verdad separada, ya extraída y verificada en esta auditoría), aplicados en un paso de despliegue distinto al de `dotnet ef database update`. **Corrección aprobada:** estos scripts no evolucionan en una línea desconectada — cada cambio futuro a una función/trigger/policy debe quedar vinculado explícitamente a la migración/release EF que lo acompaña (ej. nombrando el script con el mismo timestamp/ID que la migración C# relacionada, o referenciándolo desde el comentario de esa migración), de forma que el historial de persistencia sea uno solo y trazable, aunque el mecanismo de aplicación sea distinto.
5. **Riesgos identificados:**
   - Mapeo de tipos: `timestamp with time zone` vs `DateTime`/`DateTimeOffset` — usar `DateTimeOffset` o `DateTime` con `Kind=Utc` consistentemente (Npgsql exige uno de los dos modos, no mezclarlos).
   - Índices parciales (`WHERE compliance_status_id IS NULL`, etc.) requieren `.HasFilter("...")` explícito en Fluent — EF no los infiere de un modelo puro sin verificación contra el dump.
   - Sin roles de PostgreSQL en el dump: el pipeline de aprovisionamiento de entorno (no EF, no el dump) debe crear `procofa_owner`/`procofa_app` y otorgar los GRANTs — script de infraestructura separado, ejecutado una vez por entorno.
   - **Corrección aprobada:** `procofa_app` nunca recibe privilegios DDL, en ningún entorno. El baseline (`InitialBaseline` vacía) y toda migración incremental se ejecutan con una credencial administrativa/de migración separada (`procofa_owner` o un rol de migración dedicado) — la cadena de conexión que usa la API en runtime (con `procofa_app`) nunca es la misma que ejecuta `dotnet ef database update`.

---

## I. Estrategia RLS / UnitOfWork

Validé que el patrón físico de RLS es compatible byte a byte con lo siguiente. **Corrección aprobada:** `ITenantUnitOfWork` no crea ni gestiona una conexión externa — usa el `ProcofaDbContext` inyectado (scoped por request/job) tanto para abrir la transacción como para ejecutar el `set_config`, evitando la complejidad de una conexión Npgsql manual + `UseTransaction(...)`.

```
ITenantContext
    Guid? TenantId { get; }
    // login/refresh: resuelto desde configuración (GUID fijo de Etapa 1) antes de cualquier query
    // request autenticado: resuelto desde el claim tenant_id del JWT
    // background job: resuelto desde el payload del mensaje (outbox_messages.tenant_id)

ITenantUnitOfWork (implementación envuelve un ProcofaDbContext scoped, no una conexión externa)
    Task<T> ExecuteReadAsync<T>(Func<ProcofaDbContext,CancellationToken,Task<T>> op, CancellationToken ct)
        // await using var tx = await _db.Database.BeginTransactionAsync(ct)
        // await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('app.tenant_id', {tenantId}, true)", ct)
        // var result = await op(_db, ct)     -- el delegate usa el MISMO _db, mismo scope/transacción
        // await tx.CommitAsync(ct)
        // return result
    Task<T> ExecuteWriteAsync<T>(Func<ProcofaDbContext,CancellationToken,Task<T>> op, CancellationToken ct)
        // await using var tx = await _db.Database.BeginTransactionAsync(ct)
        // await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('app.tenant_id', {tenantId}, true)", ct)
        // var result = await op(_db, ct)
        // await _db.SaveChangesAsync(ct)
        // await tx.CommitAsync(ct)          -- ROLLBACK en catch
        // return result
```

**Punto crítico de implementación:** el `set_config(..., true)` (tercer parámetro `true` = local a la transacción) se ejecuta a través del **mismo `ProcofaDbContext`** que abrió la transacción y que luego correrá las queries — `ExecuteSqlInterpolatedAsync` sobre ese `DbContext` reutiliza automáticamente su conexión/transacción interna, sin necesidad de gestionar una `NpgsqlConnection` aparte ni `UseTransaction(...)`. Es más simple y menos frágil que construir el `DbContext` sobre una conexión externa.

- **Lecturas:** incluidas siempre en el patrón, sin excepción (RLS no distingue lectura de escritura).
- **Transacciones anidadas:** no debe abrirse un segundo `BEGIN` sobre la misma conexión dentro de un `ExecuteWriteAsync` — usar `SAVEPOINT` si se necesita anidamiento real, o rediseñar el caso de uso para una sola unidad de trabajo.
- **Background jobs:** sin `HttpContext` — el mensaje/job debe transportar `tenant_id` explícito (ya presente en `outbox_messages.tenant_id`); el worker abre su propio `ITenantUnitOfWork` por mensaje.
- **Login/refresh token:** `tenant_id` resuelto desde configuración (Etapa 1: el GUID fijo `00000000-0000-0000-0000-000000000001`) **antes** de tocar la BD — coincide exactamente con por qué la policy de `tenants` es `id = current_setting(...)` (self-referencing): sin el `SET LOCAL` previo, ni siquiera se puede leer la fila del propio tenant.
- **Endpoints globales / descarga de archivos:** transacción corta solo para metadata + autorización, `COMMIT` inmediato, el stream del archivo sale fuera de cualquier transacción abierta (sección 25 del handoff).
- **Errores/cleanup:** el UoW debe garantizar `ROLLBACK` en `finally`/`catch` para que una conexión nunca vuelva al pool con una transacción abierta — con Npgsql pooling, una conexión debe cerrarse (o su transacción resolverse) antes de regresar al pool.
- **Propiedad de fail-closed verificada:** como la policy usa `NULLIF(current_setting(...), true)`, si el `SET LOCAL` se omite por bug, el resultado es **0 filas**, nunca fuga cruzada de tenant. Útil como señal de monitoreo: un worker o endpoint que reporta sistemáticamente "0 resultados" es sospechoso de un `SET LOCAL` faltante, no necesariamente de que no haya datos.
- **Cara oculta para multi-tenant futuro:** con este diseño, ningún query puede enumerar *todos* los tenants (ni siquiera `tenants` es legible sin conocer ya un tenant_id). Para Etapa 1 no es un problema (tenant fijo por configuración). Si Etapa 2 requiere un worker verdaderamente cross-tenant (ej. un outbox dispatcher global), se necesitará un mecanismo adicional (rol `BYPASSRLS` de uso exclusivo para esa infraestructura, o una tabla de índice de tenants fuera de RLS) — no bloquea Etapa 1, queda documentado para cuando aplique.

---

## J. Autenticación y autorización

| Capa | Responsabilidad | Implementación |
|---|---|---|
| Autenticación | Verificar identidad | JWT access token (claims: `sub`=user_id, `tenant_id`, `roles[]`) + refresh token (hash almacenado en `refresh_tokens`, nunca el token crudo) + `PasswordHasher<TUser>` de ASP.NET Core (PBKDF2), reutilizado standalone |
| Policy (por rol) | "¿Puede este TIPO de usuario hacer X?" | `[Authorize(Policy = "CriteriaEvaluate")]` respaldado por `IAuthorizationHandler` que resuelve permisos desde `role_permissions` — **recomendación: no incrustar permisos en el JWT**, solo roles; resolver permisos server-side (DB o caché de corta duración) para que una revocación de permiso sea efectiva sin esperar el vencimiento del token |
| Resource-based | "¿Puede ESTE usuario actuar sobre ESTE recurso?" | Handlers específicos: `ClientAccessRequirement` (verifica `user_client_access`), `AuditTeamMembershipRequirement` (verifica `audit_team` para el alcance "asignadas" de AUDITOR_APOYO) |
| RLS | Última barrera, aislamiento físico | `SET LOCAL app.tenant_id` vía `ITenantUnitOfWork` — protege incluso ante un bug en las tres capas anteriores |

Ninguna capa sustituye a las demás — coinciden con la sección 41 del handoff. Nunca confiar solo en `[Authorize(Roles=...)]` ni solo en RLS.

---

## K. Concurrencia, idempotencia y Outbox

- **Concurrencia optimista:** `lock_version` con `IsConcurrencyToken()`; el WHERE generado por EF (`WHERE id=@id AND lock_version=@original`) con 0 filas afectadas dispara `DbUpdateConcurrencyException` → middleware la traduce a `409 ProblemDetails`. Riesgo operativo concreto encontrado (**decisión definitiva, turno 2**): `findings.finding_number` es único por `(audit_id, finding_number)` **sin secuencia ni default** — nunca calcular el "siguiente número" con `MAX+1` sin sincronización; serializar la asignación por auditoría o implementar reintento seguro ante violación de UNIQUE (ej. `SELECT ... FOR UPDATE` acotado al `audit_id`, o reintentar el INSERT con el siguiente número tras un conflicto).
- **Idempotencia:** filtro/behavior de Application que, antes de ejecutar un comando marcado idempotente, verifica `idempotency_operations` por `(tenant_id, operation_id)`; si ya existe con `status = COMPLETED`, retorna el `response_payload`/`response_status_code` guardado sin re-ejecutar. Aplica principalmente a `EvaluateCriterion` (autosave, debounce ≤3s) y a cualquier operación con reintento de red.
- **Outbox:** el insert en `outbox_messages` ocurre en la **misma transacción/SaveChanges** que la operación de dominio (ambos pasan por el mismo `ITenantUnitOfWork`). Un `BackgroundService` separado hace polling sobre `ix_outbox_pending` (`WHERE status IN ('PENDING','FAILED')`), procesa, y marca `PROCESSED`/incrementa `attempts`+`last_error`. En Etapa 1, el worker fija su `ITenantContext` al tenant único conocido antes de cada ciclo de polling (ver sección I).

---

## L. Testing

| Nivel | Qué cubre | Contra qué corre |
|---|---|---|
| Domain | Regla execution_mode/site, precondiciones de cierre (espejo del trigger), cálculo de compliance (usando `score_weight`/`included_in_score`), unicidad de `finding_number` esperada | Sin BD |
| Application | Autorización (policy + resource-based), orquestación de autosave+idempotencia, mapeo a 409, orquestación de cierre y de generación de reporte | Puertos mockeados |
| Integration | RLS (aislamiento cross-tenant real, creando 2 tenants de prueba), `enforce_same_tenant_references`, `prevent_audit_log_mutation`, `prevent_final_report_mutation`, `validate_audit_before_close`, índice único de 1-LEAD-por-auditoría, ciclo completo de Outbox | **PostgreSQL real vía Testcontainers, imagen `postgres:18`** (decisión definitiva) — usar el DDL de esquema extraído (sin el `CREATE DATABASE`/locale original); **verificado en esta auditoría que el DDL aplica sin errores** |
| API | Flujo HTTP completo con JWT+policies+RLS reales | `WebApplicationFactory` + Testcontainers |

No usar EF InMemory para nada que dependa de RLS, CHECK constraints, triggers o `SET LOCAL` — InMemory no los ejecuta y daría falsos positivos.

---

## M. Riesgos técnicos principales

1. **Locale del `CREATE DATABASE` del dump no portable a Linux** (verificado empíricamente) — mitigar generando la BD de prueba/CI con locale estándar + el DDL de esquema, no con el dump original.
2. ~~Grants de `procofa_app` insuficientes sobre 12 catálogos~~ — **resuelto (turno 2):** `programs`/`profiles`/`audit_types` serán DML-administrables a futuro vía migración controlada; los 9 restantes quedan de solo lectura por decisión, no por omisión.
3. **Disciplina de GRANTs en migraciones futuras** — como las migraciones corren con credencial administrativa (nunca `procofa_app`), cada migración que cree una tabla debe incluir el GRANT DML explícito para `procofa_app` como parte de su checklist; es un riesgo de proceso, no de diseño de privilegios.
4. **Gap de integridad cruzada** `finding`/`observation`/`evidence` vs el `audit_id` real de su `audit_criterion` — debe enforzarse en Application, no está en la BD.
5. **Carrera en `finding_number`** al crear hallazgos concurrentes en la misma auditoría.
6. **EF Core no gestiona triggers/funciones/RLS policies** — deben versionarse como scripts SQL separados, con disciplina de mantenerlos sincronizados manualmente con el modelo de dominio.
7. **Enumeración cross-tenant imposible bajo RLS tal como está diseñado** — no bloquea Etapa 1 (tenant fijo), pero condiciona el diseño de cualquier infraestructura verdaderamente multi-tenant futura (Etapa 2+).
8. **Staleness de permisos si se incrustan en el JWT** — preferir resolución server-side.
9. **Grafo de transiciones de `audit_statuses`/`corrective_action_statuses` no definido** en ningún documento — debe cerrarse antes de implementar Fase 5 (Ejecución), no bloquea Fase 1.

---

## N. Orden recomendado de implementación

Confirmo y hago concreta la Fase 1 del plan ya fijado en la sección 55 del handoff (no la reemplazo, la detallo):

1. **Foundation:** solución + 4 proyectos + 4 proyectos de test (sección E), sin código de dominio todavía.
2. **Domain esqueleto:** aggregates de la sección F (solo las entidades/invariantes de Client, AuditedCompany, Checklist/ChecklistVersion — los módulos que Fase 3-4 necesitarán primero), enums, value objects.
3. **EF Core:** Fluent Configurations + `ProcofaDbContext` + migración `InitialBaseline` vacía-pero-registrada (sección H), verificada contra la BD real.
4. **Tenant/RLS:** `ITenantContext` + `ITenantUnitOfWork` + prueba de integración que **demuestre aislamiento cross-tenant real** contra PostgreSQL en Testcontainers antes de escribir un solo endpoint.
5. **Auth:** JWT + refresh + `PasswordHasher` + policies base, corriendo a través del mismo `ITenantUnitOfWork` ya probado.
6. A partir de aquí, seguir Fase 3–12 del handoff (Clients → Audit Planning → Execution → Concurrency/Autosave → Evidence → Findings → Client portal → Reports → QA → Deployment) sin cambios respecto a lo ya definido.

---

## O. Preguntas realmente bloqueantes

Siendo estricto con el criterio pedido: **no hay ninguna pregunta que bloquee el arranque de Foundation, EF Core baseline, Tenant/RLS o Authentication.**

1. ~~Alcance real de `CATALOGS_MANAGE`~~ — **resuelto en el turno 2** (ver sección C/decisión 9): `programs`/`profiles`/`audit_types` app-administrables a futuro, el resto controlado por despliegue.
2. **Sistema operativo objetivo de staging/producción** (Windows vs Linux) — sigue abierta, no fue parte de las correcciones del turno 2. Afecta la estrategia final de locale/collation de PostgreSQL, no el código .NET. Necesaria antes de Fase 12 (Deployment), no antes de Foundation — no bloquea el trabajo actual.

---

*Metodología de verificación: el dump se parseó con `pgdumplib` (no con lectura de texto plano, dado que es un archivo custom-format binario). Los hallazgos de portabilidad de locale y de aplicabilidad íntegra del DDL fueron verificados ejecutando el esquema completo (48 tablas, 126 FK, 73 PK/UNIQUE, 36 índices, 54 triggers, 36 policies, 6 funciones) contra un PostgreSQL 16 real en Linux (único cliente/servidor disponible en el entorno de esta auditoría), con conteos post-aplicación confirmados por consulta directa a `information_schema`/`pg_catalog`. **Decisión definitiva:** el estándar de Testcontainers/CI del proyecto es `postgres:18`, no 16 — no se identificó ninguna razón para esperar un resultado distinto en 18 sobre lo verificado aquí, pero queda pendiente una confirmación puntual cuando se arme el pipeline.*
