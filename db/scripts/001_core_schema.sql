-- =============================================================================================
-- PROCOFA — Sistema Web de Gestión de Auditorías OEA/C-TPAT
-- 001_core_schema.sql — Núcleo transaccional: AuditPlan, CriterionSnapshot, AuditResult, Finding
-- Motor: PostgreSQL 18+
--
-- Decisiones de diseño:
--   * UUID nativo (gen_random_uuid(), extensión pgcrypto) como PK de todas las entidades del núcleo.
--   * Enumeraciones de dominio como TEXT + CHECK (no ENUM nativo de PostgreSQL): permite agregar
--     valores vía migración estándar sin las restricciones transaccionales de ALTER TYPE ... ADD VALUE.
--   * Concurrencia optimista (HU-11 / Sección 5.2 SRS): se apoya en la columna de sistema `xmin`,
--     mapeada por EF Core vía `UseXminAsConcurrencyToken()`. No se declara columna adicional.
--   * Inmutabilidad de criterion_snapshots: la aplicación NO debe emitir UPDATE/DELETE sobre esta
--     tabla; se recomienda reforzar con REVOKE a nivel de rol de base de datos de la app (ver 002).
-- =============================================================================================

CREATE EXTENSION IF NOT EXISTS "pgcrypto"; -- gen_random_uuid()

-- -------------------------------------------------------------------------------------------
-- Tablas de referencia externas al núcleo (Clientes, Usuarios, Checklist Maestro).
-- Declaradas aquí en forma mínima únicamente para satisfacer integridad referencial (FKs) de
-- este script. Su definición completa (columnas de negocio, auditoría, etc.) corresponde a las
-- migraciones de los módulos de Planificación / Seguridad y Roles.
-- -------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS clients (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid()
);

CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid()
);

CREATE TABLE IF NOT EXISTS checklist_master_versions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_type TEXT NOT NULL CHECK (profile_type IN ('OEA', 'CTPAT', 'BOTH')),
    version_number INT NOT NULL,
    published_at_utc TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- =============================================================================================
-- audit_plans — Aggregate root del ciclo PHVA de la auditoría (HU-01/HU-02/HU-05/HU-06)
-- =============================================================================================
CREATE TABLE audit_plans (
    id                              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id                       UUID NOT NULL REFERENCES clients(id),
    profile_type                    TEXT NOT NULL CHECK (profile_type IN ('OEA', 'CTPAT', 'BOTH')),
    checklist_master_version_id     UUID NOT NULL REFERENCES checklist_master_versions(id),
    scheduled_date                  DATE NOT NULL,
    status                          TEXT NOT NULL DEFAULT 'PLANNED'
                                         CHECK (status IN ('PLANNED', 'IN_PROGRESS', 'CLOSED', 'CANCELLED')),
    created_at_utc                  TIMESTAMPTZ NOT NULL DEFAULT now(),
    closed_at_utc                   TIMESTAMPTZ,

    -- Un plan CLOSED siempre debe traer closed_at_utc, y viceversa (invariante reforzada a nivel de BD).
    CONSTRAINT chk_audit_plans_closed_consistency
        CHECK ((status = 'CLOSED') = (closed_at_utc IS NOT NULL))
);

COMMENT ON COLUMN audit_plans.checklist_master_version_id IS
    'Fija la versión del checklist maestro vigente al momento de la carga automática (HU-03/HU-04); inmutable tras el snapshot.';

CREATE INDEX idx_audit_plans_client        ON audit_plans (client_id);
CREATE INDEX idx_audit_plans_status        ON audit_plans (status);
CREATE INDEX idx_audit_plans_scheduled_date ON audit_plans (scheduled_date);

-- Equipo asignado al plan (HU-02: exige mínimo un integrante, validado en Domain al crear).
CREATE TABLE audit_plan_team_members (
    audit_plan_id       UUID NOT NULL REFERENCES audit_plans(id) ON DELETE CASCADE,
    user_id             UUID NOT NULL REFERENCES users(id),
    assigned_at_utc     TIMESTAMPTZ NOT NULL DEFAULT now(),

    PRIMARY KEY (audit_plan_id, user_id)
);

CREATE INDEX idx_audit_plan_team_members_user ON audit_plan_team_members (user_id);

-- =============================================================================================
-- criterion_snapshots — Copia inmutable del checklist maestro para este plan (HU-03)
-- =============================================================================================
CREATE TABLE criterion_snapshots (
    id                              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    audit_plan_id                   UUID NOT NULL REFERENCES audit_plans(id) ON DELETE CASCADE,
    source_master_criterion_id      UUID NOT NULL,
    source_checklist_version        INT NOT NULL,
    section                         TEXT NOT NULL,
    code                            TEXT NOT NULL,
    description                     TEXT NOT NULL,
    is_mandatory                    BOOLEAN NOT NULL DEFAULT TRUE,
    display_order                   INT NOT NULL,
    created_at_utc                  TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT uq_criterion_snapshots_plan_code UNIQUE (audit_plan_id, code)
);

COMMENT ON TABLE criterion_snapshots IS
    'Tabla append-only a nivel de aplicación: nunca se emite UPDATE/DELETE. Reforzar con REVOKE UPDATE, DELETE sobre el rol de aplicación (ver script 002_security.sql).';

CREATE INDEX idx_criterion_snapshots_plan ON criterion_snapshots (audit_plan_id);

-- =============================================================================================
-- audit_results — Respuesta del auditor por criterio; ruta caliente de autosave (HU-08/HU-09/HU-11)
-- =============================================================================================
CREATE TABLE audit_results (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    audit_plan_id                UUID NOT NULL REFERENCES audit_plans(id) ON DELETE CASCADE,
    criterion_snapshot_id         UUID NOT NULL REFERENCES criterion_snapshots(id) ON DELETE CASCADE,
    value                        TEXT NOT NULL DEFAULT 'NOT_ANSWERED'
                                     CHECK (value IN ('NOT_ANSWERED', 'COMPLIANT', 'NON_COMPLIANT', 'NOT_APPLICABLE')),
    observations                 VARCHAR(2000),
    answered_by_user_id          UUID REFERENCES users(id),
    answered_at_utc              TIMESTAMPTZ,
    last_operation_id            UUID,   -- token de idempotencia del autosave (Sección 5.2 SRS)
    created_at_utc                TIMESTAMPTZ NOT NULL DEFAULT now(),

    -- Relación 1:1 con el criterio: garantiza upsert idempotente (un único registro por criterio).
    CONSTRAINT uq_audit_results_criterion UNIQUE (criterion_snapshot_id),

    -- No puede haber respuesta "contestada" sin usuario/timestamp, ni viceversa.
    CONSTRAINT chk_audit_results_answer_consistency
        CHECK (
            (value = 'NOT_ANSWERED' AND answered_by_user_id IS NULL AND answered_at_utc IS NULL)
            OR
            (value <> 'NOT_ANSWERED' AND answered_by_user_id IS NOT NULL AND answered_at_utc IS NOT NULL)
        )
);

COMMENT ON COLUMN audit_results.last_operation_id IS
    'GUID generado en cliente por operación de autosave; permite descartar reintentos duplicados o fuera de orden sin comparar timestamps de red.';

CREATE INDEX idx_audit_results_plan  ON audit_results (audit_plan_id);
CREATE INDEX idx_audit_results_value ON audit_results (value);

-- Concurrencia optimista (HU-11): se usa la columna de sistema `xmin` de PostgreSQL, expuesta a
-- EF Core mediante `modelBuilder.Entity<AuditResult>().UseXminAsConcurrencyToken()`. No requiere
-- columna explícita; PostgreSQL la incrementa automáticamente en cada UPDATE de la fila.

-- =============================================================================================
-- findings — No conformidades y su ciclo de cierre (HU-16 a HU-20)
-- =============================================================================================
CREATE TABLE findings (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    audit_plan_id            UUID NOT NULL REFERENCES audit_plans(id) ON DELETE CASCADE,
    audit_result_id           UUID NOT NULL REFERENCES audit_results(id),
    severity                 TEXT NOT NULL CHECK (severity IN ('MINOR', 'MAJOR', 'CRITICAL')),
    description               TEXT NOT NULL,
    status                    TEXT NOT NULL DEFAULT 'OPEN'
                                  CHECK (status IN ('OPEN', 'IN_PROGRESS', 'IN_REVIEW', 'CLOSED', 'REJECTED')),
    responsible_user_id       UUID REFERENCES users(id),
    commitment_date           DATE,
    closure_evidence_ref      TEXT,
    rejection_reason          TEXT,
    created_at_utc             TIMESTAMPTZ NOT NULL DEFAULT now(),
    closed_at_utc              TIMESTAMPTZ,

    -- Un criterio "No Cumple" genera a lo sumo un hallazgo (HU-16); refuerza la invariante de dominio.
    CONSTRAINT uq_findings_audit_result UNIQUE (audit_result_id),

    -- La fecha compromiso no puede preceder a la fecha de creación del hallazgo (HU-17).
    CONSTRAINT chk_findings_commitment_after_creation
        CHECK (commitment_date IS NULL OR commitment_date >= (created_at_utc AT TIME ZONE 'UTC')::date),

    -- Invariante de cierre: CLOSED exige closed_at_utc, y viceversa.
    CONSTRAINT chk_findings_closed_consistency
        CHECK ((status = 'CLOSED') = (closed_at_utc IS NOT NULL))
);

CREATE INDEX idx_findings_plan        ON findings (audit_plan_id);
CREATE INDEX idx_findings_status      ON findings (status);
CREATE INDEX idx_findings_responsible ON findings (responsible_user_id);

-- Soporta el semáforo de vencimiento (HU-20): hallazgos abiertos/en proceso con commitment_date vencida.
CREATE INDEX idx_findings_overdue_lookup
    ON findings (commitment_date)
    WHERE status IN ('OPEN', 'IN_PROGRESS');

-- =============================================================================================
-- Fin de 001_core_schema.sql
-- Siguiente en la secuencia de migraciones: 002_security.sql (bitácora inmutable, roles de BD,
-- REVOKE UPDATE/DELETE sobre criterion_snapshots) — fuera de alcance de este script.
-- =============================================================================================
