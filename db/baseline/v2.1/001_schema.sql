-- =============================================================================
-- 001_schema.sql — Esquema físico completo (48 tablas) del baseline V2.1
-- =============================================================================
-- Generado a partir de procofa_bdFinal.sql (dump PGDMP real de
-- procofa_audit_db, server_version=18.3) — SOLO estructura (DDL). Excluye
-- deliberadamente:
--   * El CREATE DATABASE original: usaba
--     LOCALE = 'Spanish_Mexico.1252' (Windows-only, no portable a la imagen
--     postgres:18 de Linux que usan Testcontainers/CI) — se deja que el
--     motor destino use su propio locale por defecto (ver README.md).
--   * Cualquier password/secreto — no existe ninguno en el DDL en sí.
--
-- Orden de ejecución (respeta dependencias): extensiones -> tablas (con
-- FORCE ROW LEVEL SECURITY inline, tal como las emite pg_dump) ->
-- constraints PK/UNIQUE -> constraints FK -> índices -> funciones ->
-- triggers (dependen de que las funciones ya existan).
--
-- Ejecutar como el superusuario por defecto del contenedor Testcontainers
-- (usualmente "postgres") — 002_security.sql reasigna el ownership de las
-- tablas a procofa_owner después de correr este script.
-- =============================================================================

-- ---- Extensiones ----
-- desc=EXTENSION tag='pgcrypto' namespace='' oid=26707 table_oid=3079 dump_id=2
CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;

-- desc=EXTENSION tag='uuid-ossp' namespace='' oid=26745 table_oid=3079 dump_id=3
CREATE EXTENSION IF NOT EXISTS "uuid-ossp" WITH SCHEMA public;

-- ---- Tablas (48) — incluye FORCE ROW LEVEL SECURITY inline donde aplica ----
-- desc=TABLE tag='access_logs' namespace='public' oid=28224 table_oid=1259 dump_id=268
CREATE TABLE public.access_logs (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    user_id uuid,
    attempted_email character varying(255),
    event_type character varying(40) NOT NULL,
    ip_address inet,
    user_agent text,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT access_logs_event_type_check CHECK (((event_type)::text = ANY ((ARRAY['LOGIN_SUCCESS'::character varying, 'LOGIN_FAILURE'::character varying, 'LOGOUT'::character varying, 'PASSWORD_RESET_REQUEST'::character varying, 'PASSWORD_RESET_SUCCESS'::character varying, 'ACCOUNT_LOCKED'::character varying])::text[])))
);

ALTER TABLE ONLY public.access_logs FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='audit_checklists' namespace='public' oid=27471 table_oid=1259 dump_id=250
CREATE TABLE public.audit_checklists (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    audit_id uuid NOT NULL,
    checklist_version_id uuid NOT NULL,
    assigned_at_utc timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY public.audit_checklists FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='audit_criteria' namespace='public' oid=27533 table_oid=1259 dump_id=252
CREATE TABLE public.audit_criteria (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    audit_id uuid NOT NULL,
    audit_checklist_id uuid NOT NULL,
    criterion_id uuid NOT NULL,
    compliance_status_id uuid,
    criterion_code_snapshot character varying(80) NOT NULL,
    question_snapshot text NOT NULL,
    normative_reference_snapshot text,
    is_mandatory_snapshot boolean NOT NULL,
    audited_response text,
    identified_risk text,
    recommendation text,
    evaluated_by_user_id uuid,
    evaluated_at_utc timestamp with time zone,
    lock_version bigint DEFAULT 1 NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT audit_criteria_lock_version_check CHECK ((lock_version > 0))
);

ALTER TABLE ONLY public.audit_criteria FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='audit_document_requests' namespace='public' oid=27627 table_oid=1259 dump_id=254
CREATE TABLE public.audit_document_requests (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    audit_id uuid NOT NULL,
    requested_by_user_id uuid NOT NULL,
    title character varying(200) NOT NULL,
    description text,
    due_date date,
    status character varying(30) DEFAULT 'PENDIENTE'::character varying NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT audit_document_requests_status_check CHECK (((status)::text = ANY ((ARRAY['PENDIENTE'::character varying, 'ENTREGADO'::character varying, 'VALIDADO'::character varying, 'RECHAZADO'::character varying, 'CANCELADO'::character varying])::text[])))
);

ALTER TABLE ONLY public.audit_document_requests FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='audit_evidences' namespace='public' oid=27662 table_oid=1259 dump_id=255
CREATE TABLE public.audit_evidences (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    audit_id uuid NOT NULL,
    audit_criterion_id uuid,
    finding_id uuid,
    corrective_action_id uuid,
    document_request_id uuid,
    uploaded_by_user_id uuid NOT NULL,
    evidence_type character varying(30) NOT NULL,
    original_file_name character varying(255) NOT NULL,
    storage_key text NOT NULL,
    mime_type character varying(150),
    file_size_bytes bigint,
    sha256_hex character varying(64),
    description text,
    is_report_relevant boolean DEFAULT true NOT NULL,
    include_in_report boolean DEFAULT true NOT NULL,
    include_as_annex boolean DEFAULT false NOT NULL,
    annex_order integer,
    caption text,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT audit_evidences_annex_order_check CHECK (((annex_order IS NULL) OR (annex_order > 0))),
    CONSTRAINT audit_evidences_evidence_type_check CHECK (((evidence_type)::text = ANY ((ARRAY['FOTO'::character varying, 'PDF'::character varying, 'WORD'::character varying, 'EXCEL'::character varying, 'IMAGEN'::character varying, 'CAPTURA'::character varying, 'REGISTRO'::character varying, 'OTRO'::character varying])::text[]))),
    CONSTRAINT audit_evidences_file_size_bytes_check CHECK (((file_size_bytes IS NULL) OR (file_size_bytes >= 0)))
);

ALTER TABLE ONLY public.audit_evidences FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='audit_logs' namespace='public' oid=28195 table_oid=1259 dump_id=267
CREATE TABLE public.audit_logs (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    user_id uuid,
    role_code character varying(30),
    audit_id uuid,
    entity_name character varying(80) NOT NULL,
    entity_id uuid,
    action character varying(80) NOT NULL,
    old_values jsonb,
    new_values jsonb,
    ip_address inet,
    user_agent text,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY public.audit_logs FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='audit_programs' namespace='public' oid=27448 table_oid=1259 dump_id=249
CREATE TABLE public.audit_programs (
    tenant_id uuid NOT NULL,
    audit_id uuid NOT NULL,
    program_id uuid NOT NULL
);

ALTER TABLE ONLY public.audit_programs FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='audit_reports' namespace='public' oid=28014 table_oid=1259 dump_id=262
CREATE TABLE public.audit_reports (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    audit_id uuid NOT NULL,
    report_template_version_id uuid,
    report_type character varying(30) NOT NULL,
    version_number integer DEFAULT 1 NOT NULL,
    format character varying(10) NOT NULL,
    status character varying(20) DEFAULT 'DRAFT'::character varying NOT NULL,
    storage_key text NOT NULL,
    sha256_hex character varying(64),
    generated_by_user_id uuid NOT NULL,
    validated_by_user_id uuid,
    generated_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    validated_at_utc timestamp with time zone,
    CONSTRAINT audit_reports_format_check CHECK (((format)::text = ANY ((ARRAY['PDF'::character varying, 'DOCX'::character varying, 'XLSX'::character varying])::text[]))),
    CONSTRAINT audit_reports_report_type_check CHECK (((report_type)::text = ANY ((ARRAY['FINAL'::character varying, 'EJECUTIVO'::character varying, 'HALLAZGOS'::character varying, 'ACCIONES'::character varying, 'SEGUIMIENTO'::character varying])::text[]))),
    CONSTRAINT audit_reports_status_check CHECK (((status)::text = ANY ((ARRAY['DRAFT'::character varying, 'FINAL'::character varying, 'VOID'::character varying])::text[]))),
    CONSTRAINT audit_reports_version_number_check CHECK ((version_number > 0))
);

ALTER TABLE ONLY public.audit_reports FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='audit_results' namespace='public' oid=27894 table_oid=1259 dump_id=259
CREATE TABLE public.audit_results (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    audit_id uuid NOT NULL,
    executive_summary text,
    general_result text,
    conclusions text,
    general_recommendations text,
    compliance_percentage numeric(5,2),
    evaluated_criteria_count integer DEFAULT 0 NOT NULL,
    compliant_criteria_count integer DEFAULT 0 NOT NULL,
    partially_compliant_criteria_count integer DEFAULT 0 NOT NULL,
    non_compliant_criteria_count integer DEFAULT 0 NOT NULL,
    not_applicable_criteria_count integer DEFAULT 0 NOT NULL,
    finalized_by_user_id uuid,
    finalized_at_utc timestamp with time zone,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT audit_results_compliance_percentage_check CHECK (((compliance_percentage IS NULL) OR ((compliance_percentage >= (0)::numeric) AND (compliance_percentage <= (100)::numeric)))),
    CONSTRAINT audit_results_compliant_criteria_count_check CHECK ((compliant_criteria_count >= 0)),
    CONSTRAINT audit_results_evaluated_criteria_count_check CHECK ((evaluated_criteria_count >= 0)),
    CONSTRAINT audit_results_non_compliant_criteria_count_check CHECK ((non_compliant_criteria_count >= 0)),
    CONSTRAINT audit_results_not_applicable_criteria_count_check CHECK ((not_applicable_criteria_count >= 0)),
    CONSTRAINT audit_results_partially_compliant_criteria_count_check CHECK ((partially_compliant_criteria_count >= 0))
);

ALTER TABLE ONLY public.audit_results FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='audit_signatories' namespace='public' oid=28066 table_oid=1259 dump_id=263
CREATE TABLE public.audit_signatories (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    audit_id uuid NOT NULL,
    user_id uuid,
    client_contact_id uuid,
    signer_name character varying(200) NOT NULL,
    signer_role character varying(150),
    signer_type character varying(30) NOT NULL,
    signature_storage_key text,
    signed_at_utc timestamp with time zone,
    sort_order integer DEFAULT 0 NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT audit_signatories_signer_type_check CHECK (((signer_type)::text = ANY ((ARRAY['AUDITOR_LIDER'::character varying, 'AUDITOR'::character varying, 'CLIENTE'::character varying, 'RESPONSABLE'::character varying])::text[]))),
    CONSTRAINT ck_audit_signatory_source CHECK (((user_id IS NOT NULL) OR (client_contact_id IS NOT NULL) OR (signer_name IS NOT NULL)))
);

ALTER TABLE ONLY public.audit_signatories FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='audit_statuses' namespace='public' oid=26977 table_oid=1259 dump_id=232
CREATE TABLE public.audit_statuses (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code character varying(40) NOT NULL,
    name character varying(100) NOT NULL,
    sort_order integer DEFAULT 0 NOT NULL,
    is_terminal boolean DEFAULT false NOT NULL
);

-- desc=TABLE tag='audit_team' namespace='public' oid=27500 table_oid=1259 dump_id=251
CREATE TABLE public.audit_team (
    tenant_id uuid NOT NULL,
    audit_id uuid NOT NULL,
    user_id uuid NOT NULL,
    audit_role character varying(20) NOT NULL,
    assigned_by_user_id uuid,
    assigned_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT audit_team_audit_role_check CHECK (((audit_role)::text = ANY ((ARRAY['LEAD'::character varying, 'SUPPORT'::character varying])::text[])))
);

ALTER TABLE ONLY public.audit_team FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='audit_types' namespace='public' oid=26962 table_oid=1259 dump_id=231
CREATE TABLE public.audit_types (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code character varying(40) NOT NULL,
    name character varying(150) NOT NULL,
    description text,
    is_active boolean DEFAULT true NOT NULL
);

-- desc=TABLE tag='audited_companies' namespace='public' oid=27109 table_oid=1259 dump_id=240
CREATE TABLE public.audited_companies (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    client_id uuid NOT NULL,
    default_profile_id uuid,
    legal_name character varying(200) NOT NULL,
    trade_name character varying(200),
    tax_id character varying(30),
    industry character varying(150),
    company_type character varying(100),
    is_client_company boolean DEFAULT false NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY public.audited_companies FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='audits' namespace='public' oid=27377 table_oid=1259 dump_id=248
CREATE TABLE public.audits (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    folio character varying(50) NOT NULL,
    client_id uuid NOT NULL,
    audited_company_id uuid NOT NULL,
    company_site_id uuid,
    audit_type_id uuid NOT NULL,
    profile_id uuid NOT NULL,
    status_id uuid NOT NULL,
    objective text NOT NULL,
    scope text NOT NULL,
    methodology text,
    scheduled_date date NOT NULL,
    started_at_utc timestamp with time zone,
    finished_at_utc timestamp with time zone,
    closed_at_utc timestamp with time zone,
    created_by_user_id uuid NOT NULL,
    validated_by_user_id uuid,
    validated_at_utc timestamp with time zone,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    execution_mode character varying(20) NOT NULL,
    CONSTRAINT ck_audits_execution_mode CHECK (((execution_mode)::text = ANY ((ARRAY['ONSITE'::character varying, 'REMOTE'::character varying, 'HYBRID'::character varying])::text[])))
);

ALTER TABLE ONLY public.audits FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='checklist_sections' namespace='public' oid=27323 table_oid=1259 dump_id=246
CREATE TABLE public.checklist_sections (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    checklist_version_id uuid NOT NULL,
    code character varying(50),
    name character varying(200) NOT NULL,
    description text,
    sort_order integer DEFAULT 0 NOT NULL
);

ALTER TABLE ONLY public.checklist_sections FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='checklist_versions' namespace='public' oid=27285 table_oid=1259 dump_id=245
CREATE TABLE public.checklist_versions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    checklist_id uuid NOT NULL,
    version_number integer NOT NULL,
    status character varying(20) DEFAULT 'DRAFT'::character varying NOT NULL,
    change_notes text,
    published_at_utc timestamp with time zone,
    created_by_user_id uuid NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT checklist_versions_status_check CHECK (((status)::text = ANY ((ARRAY['DRAFT'::character varying, 'PUBLISHED'::character varying, 'RETIRED'::character varying])::text[]))),
    CONSTRAINT checklist_versions_version_number_check CHECK ((version_number > 0))
);

ALTER TABLE ONLY public.checklist_versions FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='checklists' namespace='public' oid=27240 table_oid=1259 dump_id=244
CREATE TABLE public.checklists (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    program_id uuid NOT NULL,
    profile_id uuid NOT NULL,
    audit_type_id uuid,
    name character varying(200) NOT NULL,
    description text,
    is_active boolean DEFAULT true NOT NULL,
    created_by_user_id uuid NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY public.checklists FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='client_contacts' namespace='public' oid=27176 table_oid=1259 dump_id=242
CREATE TABLE public.client_contacts (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    client_id uuid NOT NULL,
    audited_company_id uuid,
    first_name character varying(100) NOT NULL,
    last_name character varying(100) NOT NULL,
    job_title character varying(120),
    email character varying(255),
    phone character varying(30),
    is_active boolean DEFAULT true NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY public.client_contacts FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='client_programs' namespace='public' oid=27086 table_oid=1259 dump_id=239
CREATE TABLE public.client_programs (
    tenant_id uuid NOT NULL,
    client_id uuid NOT NULL,
    program_id uuid NOT NULL
);

ALTER TABLE ONLY public.client_programs FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='clients' namespace='public' oid=27063 table_oid=1259 dump_id=238
CREATE TABLE public.clients (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    legal_name character varying(200) NOT NULL,
    trade_name character varying(200),
    tax_id character varying(30),
    industry character varying(150),
    company_type character varying(100),
    notes text,
    is_active boolean DEFAULT true NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY public.clients FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='company_sites' namespace='public' oid=27145 table_oid=1259 dump_id=241
CREATE TABLE public.company_sites (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    audited_company_id uuid NOT NULL,
    name character varying(150) NOT NULL,
    address_line1 character varying(200) NOT NULL,
    address_line2 character varying(200),
    city character varying(120),
    state_region character varying(120),
    postal_code character varying(20),
    country character varying(100) DEFAULT 'México'::character varying NOT NULL,
    latitude numeric(9,6),
    longitude numeric(9,6),
    is_active boolean DEFAULT true NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY public.company_sites FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='compliance_statuses' namespace='public' oid=26992 table_oid=1259 dump_id=233
CREATE TABLE public.compliance_statuses (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code character varying(40) NOT NULL,
    name character varying(100) NOT NULL,
    score_weight numeric(5,2),
    included_in_score boolean DEFAULT true NOT NULL,
    sort_order integer DEFAULT 0 NOT NULL
);

-- desc=TABLE tag='corrective_action_statuses' namespace='public' oid=27048 table_oid=1259 dump_id=237
CREATE TABLE public.corrective_action_statuses (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code character varying(40) NOT NULL,
    name character varying(100) NOT NULL,
    is_closed boolean DEFAULT false NOT NULL,
    sort_order integer DEFAULT 0 NOT NULL
);

-- desc=TABLE tag='corrective_actions' namespace='public' oid=27791 table_oid=1259 dump_id=257
CREATE TABLE public.corrective_actions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    finding_id uuid NOT NULL,
    status_id uuid NOT NULL,
    description text NOT NULL,
    responsible_user_id uuid,
    responsible_contact_id uuid,
    commitment_date date NOT NULL,
    completion_notes text,
    completed_at_utc timestamp with time zone,
    validated_by_user_id uuid,
    validated_at_utc timestamp with time zone,
    created_by_user_id uuid NOT NULL,
    lock_version bigint DEFAULT 1 NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT ck_corrective_action_responsible CHECK (((responsible_user_id IS NOT NULL) OR (responsible_contact_id IS NOT NULL))),
    CONSTRAINT corrective_actions_lock_version_check CHECK ((lock_version > 0))
);

ALTER TABLE ONLY public.corrective_actions FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='criteria' namespace='public' oid=27347 table_oid=1259 dump_id=247
CREATE TABLE public.criteria (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    checklist_section_id uuid NOT NULL,
    code character varying(80) NOT NULL,
    audit_question text NOT NULL,
    auditor_interpretation text,
    expected_evidence text,
    evidence_type character varying(80),
    importance_level character varying(20),
    normative_reference text,
    evaluation_recommendation text,
    is_mandatory boolean DEFAULT true NOT NULL,
    sort_order integer DEFAULT 0 NOT NULL,
    CONSTRAINT criteria_importance_level_check CHECK (((importance_level IS NULL) OR ((importance_level)::text = ANY ((ARRAY['ALTA'::character varying, 'MEDIA'::character varying, 'BAJA'::character varying])::text[]))))
);

ALTER TABLE ONLY public.criteria FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='finding_followups' namespace='public' oid=27849 table_oid=1259 dump_id=258
CREATE TABLE public.finding_followups (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    finding_id uuid NOT NULL,
    corrective_action_id uuid,
    author_user_id uuid NOT NULL,
    event_type character varying(50) NOT NULL,
    comment text,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY public.finding_followups FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='finding_priorities' namespace='public' oid=27020 table_oid=1259 dump_id=235
CREATE TABLE public.finding_priorities (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code character varying(20) NOT NULL,
    name character varying(60) NOT NULL,
    sort_order integer DEFAULT 0 NOT NULL
);

-- desc=TABLE tag='finding_statuses' namespace='public' oid=27033 table_oid=1259 dump_id=236
CREATE TABLE public.finding_statuses (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code character varying(40) NOT NULL,
    name character varying(100) NOT NULL,
    is_closed boolean DEFAULT false NOT NULL,
    sort_order integer DEFAULT 0 NOT NULL
);

-- desc=TABLE tag='finding_types' namespace='public' oid=27007 table_oid=1259 dump_id=234
CREATE TABLE public.finding_types (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code character varying(40) NOT NULL,
    name character varying(120) NOT NULL,
    description text
);

-- desc=TABLE tag='findings' namespace='public' oid=27713 table_oid=1259 dump_id=256
CREATE TABLE public.findings (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    audit_id uuid NOT NULL,
    audit_criterion_id uuid NOT NULL,
    finding_number integer NOT NULL,
    finding_type_id uuid NOT NULL,
    priority_id uuid NOT NULL,
    status_id uuid NOT NULL,
    title character varying(200),
    description text NOT NULL,
    observed_evidence text,
    risk_impact text,
    violated_requirement text,
    recommendation text,
    responsible_user_id uuid,
    responsible_contact_id uuid,
    commitment_date date,
    created_by_user_id uuid NOT NULL,
    validated_by_user_id uuid,
    validated_at_utc timestamp with time zone,
    closed_at_utc timestamp with time zone,
    lock_version bigint DEFAULT 1 NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT findings_finding_number_check CHECK ((finding_number > 0)),
    CONSTRAINT findings_lock_version_check CHECK ((lock_version > 0))
);

ALTER TABLE ONLY public.findings FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='idempotency_operations' namespace='public' oid=28138 table_oid=1259 dump_id=265
CREATE TABLE public.idempotency_operations (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    user_id uuid NOT NULL,
    operation_id uuid NOT NULL,
    operation_type character varying(80) NOT NULL,
    request_hash character varying(64),
    resource_type character varying(80),
    resource_id uuid,
    status character varying(20) DEFAULT 'IN_PROGRESS'::character varying NOT NULL,
    response_status_code integer,
    response_payload jsonb,
    expires_at_utc timestamp with time zone,
    completed_at_utc timestamp with time zone,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT idempotency_operations_status_check CHECK (((status)::text = ANY ((ARRAY['IN_PROGRESS'::character varying, 'COMPLETED'::character varying, 'FAILED'::character varying])::text[])))
);

ALTER TABLE ONLY public.idempotency_operations FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='notifications' namespace='public' oid=28107 table_oid=1259 dump_id=264
CREATE TABLE public.notifications (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    user_id uuid NOT NULL,
    channel character varying(20) DEFAULT 'INTERNAL'::character varying NOT NULL,
    notification_type character varying(50) NOT NULL,
    title character varying(200) NOT NULL,
    message text NOT NULL,
    related_entity character varying(50),
    related_entity_id uuid,
    is_read boolean DEFAULT false NOT NULL,
    read_at_utc timestamp with time zone,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT notifications_channel_check CHECK (((channel)::text = ANY ((ARRAY['INTERNAL'::character varying, 'EMAIL'::character varying])::text[])))
);

ALTER TABLE ONLY public.notifications FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='observations' namespace='public' oid=27588 table_oid=1259 dump_id=253
CREATE TABLE public.observations (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    audit_id uuid NOT NULL,
    audit_criterion_id uuid NOT NULL,
    author_user_id uuid NOT NULL,
    observation_type character varying(30) DEFAULT 'AUDITOR'::character varying NOT NULL,
    description text NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT observations_observation_type_check CHECK (((observation_type)::text = ANY ((ARRAY['AUDITOR'::character varying, 'CLIENTE'::character varying, 'INTERNA'::character varying])::text[])))
);

ALTER TABLE ONLY public.observations FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='outbox_messages' namespace='public' oid=28168 table_oid=1259 dump_id=266
CREATE TABLE public.outbox_messages (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    event_type character varying(120) NOT NULL,
    aggregate_type character varying(80),
    aggregate_id uuid,
    payload jsonb NOT NULL,
    status character varying(20) DEFAULT 'PENDING'::character varying NOT NULL,
    attempts integer DEFAULT 0 NOT NULL,
    available_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    processed_at_utc timestamp with time zone,
    last_error text,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT outbox_messages_attempts_check CHECK ((attempts >= 0)),
    CONSTRAINT outbox_messages_status_check CHECK (((status)::text = ANY ((ARRAY['PENDING'::character varying, 'PROCESSING'::character varying, 'PROCESSED'::character varying, 'FAILED'::character varying])::text[])))
);

ALTER TABLE ONLY public.outbox_messages FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='password_reset_tokens' namespace='public' oid=26880 table_oid=1259 dump_id=227
CREATE TABLE public.password_reset_tokens (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    user_id uuid NOT NULL,
    token_hash text NOT NULL,
    expires_at_utc timestamp with time zone NOT NULL,
    used_at_utc timestamp with time zone,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY public.password_reset_tokens FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='permissions' namespace='public' oid=26787 table_oid=1259 dump_id=223
CREATE TABLE public.permissions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code character varying(80) NOT NULL,
    name character varying(150) NOT NULL,
    description text
);

-- desc=TABLE tag='profiles' namespace='public' oid=26947 table_oid=1259 dump_id=230
CREATE TABLE public.profiles (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code character varying(40) NOT NULL,
    name character varying(120) NOT NULL,
    description text,
    is_active boolean DEFAULT true NOT NULL
);

-- desc=TABLE tag='programs' namespace='public' oid=26932 table_oid=1259 dump_id=229
CREATE TABLE public.programs (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code character varying(30) NOT NULL,
    name character varying(100) NOT NULL,
    description text,
    is_active boolean DEFAULT true NOT NULL
);

-- desc=TABLE tag='refresh_tokens' namespace='public' oid=26905 table_oid=1259 dump_id=228
CREATE TABLE public.refresh_tokens (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    user_id uuid NOT NULL,
    token_hash text NOT NULL,
    expires_at_utc timestamp with time zone NOT NULL,
    revoked_at_utc timestamp with time zone,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY public.refresh_tokens FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='report_template_versions' namespace='public' oid=27975 table_oid=1259 dump_id=261
CREATE TABLE public.report_template_versions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    report_template_id uuid NOT NULL,
    version_number integer NOT NULL,
    status character varying(20) DEFAULT 'DRAFT'::character varying NOT NULL,
    template_storage_key text NOT NULL,
    configuration_json jsonb,
    change_notes text,
    published_at_utc timestamp with time zone,
    created_by_user_id uuid NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT report_template_versions_status_check CHECK (((status)::text = ANY ((ARRAY['DRAFT'::character varying, 'PUBLISHED'::character varying, 'RETIRED'::character varying])::text[]))),
    CONSTRAINT report_template_versions_version_number_check CHECK ((version_number > 0))
);

ALTER TABLE ONLY public.report_template_versions FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='report_templates' namespace='public' oid=27942 table_oid=1259 dump_id=260
CREATE TABLE public.report_templates (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    code character varying(50) NOT NULL,
    name character varying(150) NOT NULL,
    report_type character varying(30) NOT NULL,
    description text,
    is_active boolean DEFAULT true NOT NULL,
    created_by_user_id uuid NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT report_templates_report_type_check CHECK (((report_type)::text = ANY ((ARRAY['FINAL'::character varying, 'EJECUTIVO'::character varying, 'HALLAZGOS'::character varying, 'ACCIONES'::character varying, 'SEGUIMIENTO'::character varying])::text[])))
);

ALTER TABLE ONLY public.report_templates FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='role_permissions' namespace='public' oid=26800 table_oid=1259 dump_id=224
CREATE TABLE public.role_permissions (
    role_id uuid NOT NULL,
    permission_id uuid NOT NULL
);

-- desc=TABLE tag='roles' namespace='public' oid=26774 table_oid=1259 dump_id=222
CREATE TABLE public.roles (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code character varying(30) NOT NULL,
    name character varying(100) NOT NULL,
    description text
);

-- desc=TABLE tag='tenants' namespace='public' oid=26757 table_oid=1259 dump_id=221
CREATE TABLE public.tenants (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    name character varying(150) NOT NULL,
    slug character varying(80) NOT NULL,
    legal_name character varying(200),
    tax_id character varying(30),
    is_active boolean DEFAULT true NOT NULL,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY public.tenants FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='user_client_access' namespace='public' oid=27210 table_oid=1259 dump_id=243
CREATE TABLE public.user_client_access (
    tenant_id uuid NOT NULL,
    user_id uuid NOT NULL,
    client_id uuid NOT NULL,
    granted_by_user_id uuid,
    granted_at_utc timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY public.user_client_access FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='user_roles' namespace='public' oid=26850 table_oid=1259 dump_id=226
CREATE TABLE public.user_roles (
    tenant_id uuid NOT NULL,
    user_id uuid NOT NULL,
    role_id uuid NOT NULL,
    assigned_by_user_id uuid,
    assigned_at_utc timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY public.user_roles FORCE ROW LEVEL SECURITY;

-- desc=TABLE tag='users' namespace='public' oid=26817 table_oid=1259 dump_id=225
CREATE TABLE public.users (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    email character varying(255) NOT NULL,
    normalized_email character varying(255) NOT NULL,
    password_hash text NOT NULL,
    first_name character varying(100) NOT NULL,
    last_name character varying(100) NOT NULL,
    phone character varying(30),
    is_active boolean DEFAULT true NOT NULL,
    must_change_password boolean DEFAULT false NOT NULL,
    failed_login_attempts integer DEFAULT 0 NOT NULL,
    locked_until_utc timestamp with time zone,
    last_login_at_utc timestamp with time zone,
    created_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    updated_at_utc timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT users_failed_login_attempts_check CHECK ((failed_login_attempts >= 0))
);

ALTER TABLE ONLY public.users FORCE ROW LEVEL SECURITY;

-- ---- Constraints PK / UNIQUE (73) ----
-- desc=CONSTRAINT tag='access_logs access_logs_pkey' namespace='public' oid=28237 table_oid=2606 dump_id=5456
ALTER TABLE ONLY public.access_logs
    ADD CONSTRAINT access_logs_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='audit_checklists audit_checklists_pkey' namespace='public' oid=27482 table_oid=2606 dump_id=5383
ALTER TABLE ONLY public.audit_checklists
    ADD CONSTRAINT audit_checklists_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='audit_checklists uq_audit_checklist_version' namespace='public' oid=27484 table_oid=2606 dump_id=5385
ALTER TABLE ONLY public.audit_checklists
    ADD CONSTRAINT uq_audit_checklist_version UNIQUE (audit_id, checklist_version_id);

-- desc=CONSTRAINT tag='audit_criteria audit_criteria_pkey' namespace='public' oid=27555 table_oid=2606 dump_id=5391
ALTER TABLE ONLY public.audit_criteria
    ADD CONSTRAINT audit_criteria_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='audit_criteria uq_audit_criterion' namespace='public' oid=27557 table_oid=2606 dump_id=5395
ALTER TABLE ONLY public.audit_criteria
    ADD CONSTRAINT uq_audit_criterion UNIQUE (audit_id, criterion_id);

-- desc=CONSTRAINT tag='audit_document_requests audit_document_requests_pkey' namespace='public' oid=27646 table_oid=2606 dump_id=5400
ALTER TABLE ONLY public.audit_document_requests
    ADD CONSTRAINT audit_document_requests_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='audit_evidences audit_evidences_pkey' namespace='public' oid=27687 table_oid=2606 dump_id=5402
ALTER TABLE ONLY public.audit_evidences
    ADD CONSTRAINT audit_evidences_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='audit_logs audit_logs_pkey' namespace='public' oid=28208 table_oid=2606 dump_id=5452
ALTER TABLE ONLY public.audit_logs
    ADD CONSTRAINT audit_logs_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='audit_programs pk_audit_programs' namespace='public' oid=27455 table_oid=2606 dump_id=5381
ALTER TABLE ONLY public.audit_programs
    ADD CONSTRAINT pk_audit_programs PRIMARY KEY (audit_id, program_id);

-- desc=CONSTRAINT tag='audit_reports audit_reports_pkey' namespace='public' oid=28038 table_oid=2606 dump_id=5434
ALTER TABLE ONLY public.audit_reports
    ADD CONSTRAINT audit_reports_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='audit_reports uq_audit_report_version' namespace='public' oid=28040 table_oid=2606 dump_id=5436
ALTER TABLE ONLY public.audit_reports
    ADD CONSTRAINT uq_audit_report_version UNIQUE (audit_id, report_type, version_number, format);

-- desc=CONSTRAINT tag='audit_results audit_results_audit_id_key' namespace='public' oid=27926 table_oid=2606 dump_id=5419
ALTER TABLE ONLY public.audit_results
    ADD CONSTRAINT audit_results_audit_id_key UNIQUE (audit_id);

-- desc=CONSTRAINT tag='audit_results audit_results_pkey' namespace='public' oid=27924 table_oid=2606 dump_id=5421
ALTER TABLE ONLY public.audit_results
    ADD CONSTRAINT audit_results_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='audit_signatories audit_signatories_pkey' namespace='public' oid=28086 table_oid=2606 dump_id=5438
ALTER TABLE ONLY public.audit_signatories
    ADD CONSTRAINT audit_signatories_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='audit_statuses audit_statuses_code_key' namespace='public' oid=26991 table_oid=2606 dump_id=5317
ALTER TABLE ONLY public.audit_statuses
    ADD CONSTRAINT audit_statuses_code_key UNIQUE (code);

-- desc=CONSTRAINT tag='audit_statuses audit_statuses_pkey' namespace='public' oid=26989 table_oid=2606 dump_id=5319
ALTER TABLE ONLY public.audit_statuses
    ADD CONSTRAINT audit_statuses_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='audit_team pk_audit_team' namespace='public' oid=27511 table_oid=2606 dump_id=5388
ALTER TABLE ONLY public.audit_team
    ADD CONSTRAINT pk_audit_team PRIMARY KEY (audit_id, user_id);

-- desc=CONSTRAINT tag='audit_types audit_types_code_key' namespace='public' oid=26976 table_oid=2606 dump_id=5313
ALTER TABLE ONLY public.audit_types
    ADD CONSTRAINT audit_types_code_key UNIQUE (code);

-- desc=CONSTRAINT tag='audit_types audit_types_pkey' namespace='public' oid=26974 table_oid=2606 dump_id=5315
ALTER TABLE ONLY public.audit_types
    ADD CONSTRAINT audit_types_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='audited_companies audited_companies_pkey' namespace='public' oid=27128 table_oid=2606 dump_id=5347
ALTER TABLE ONLY public.audited_companies
    ADD CONSTRAINT audited_companies_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='audits audits_pkey' namespace='public' oid=27400 table_oid=2606 dump_id=5374
ALTER TABLE ONLY public.audits
    ADD CONSTRAINT audits_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='audits uq_audits_tenant_folio' namespace='public' oid=27402 table_oid=2606 dump_id=5379
ALTER TABLE ONLY public.audits
    ADD CONSTRAINT uq_audits_tenant_folio UNIQUE (tenant_id, folio);

-- desc=CONSTRAINT tag='checklist_sections checklist_sections_pkey' namespace='public' oid=27336 table_oid=2606 dump_id=5367
ALTER TABLE ONLY public.checklist_sections
    ADD CONSTRAINT checklist_sections_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='checklist_versions checklist_versions_pkey' namespace='public' oid=27305 table_oid=2606 dump_id=5362
ALTER TABLE ONLY public.checklist_versions
    ADD CONSTRAINT checklist_versions_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='checklist_versions uq_checklist_version' namespace='public' oid=27307 table_oid=2606 dump_id=5365
ALTER TABLE ONLY public.checklist_versions
    ADD CONSTRAINT uq_checklist_version UNIQUE (checklist_id, version_number);

-- desc=CONSTRAINT tag='checklists checklists_pkey' namespace='public' oid=27259 table_oid=2606 dump_id=5359
ALTER TABLE ONLY public.checklists
    ADD CONSTRAINT checklists_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='client_contacts client_contacts_pkey' namespace='public' oid=27194 table_oid=2606 dump_id=5354
ALTER TABLE ONLY public.client_contacts
    ADD CONSTRAINT client_contacts_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='client_programs pk_client_programs' namespace='public' oid=27093 table_oid=2606 dump_id=5345
ALTER TABLE ONLY public.client_programs
    ADD CONSTRAINT pk_client_programs PRIMARY KEY (client_id, program_id);

-- desc=CONSTRAINT tag='clients clients_pkey' namespace='public' oid=27079 table_oid=2606 dump_id=5341
ALTER TABLE ONLY public.clients
    ADD CONSTRAINT clients_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='company_sites company_sites_pkey' namespace='public' oid=27165 table_oid=2606 dump_id=5351
ALTER TABLE ONLY public.company_sites
    ADD CONSTRAINT company_sites_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='compliance_statuses compliance_statuses_code_key' namespace='public' oid=27006 table_oid=2606 dump_id=5321
ALTER TABLE ONLY public.compliance_statuses
    ADD CONSTRAINT compliance_statuses_code_key UNIQUE (code);

-- desc=CONSTRAINT tag='compliance_statuses compliance_statuses_pkey' namespace='public' oid=27004 table_oid=2606 dump_id=5323
ALTER TABLE ONLY public.compliance_statuses
    ADD CONSTRAINT compliance_statuses_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='corrective_action_statuses corrective_action_statuses_code_key' namespace='public' oid=27062 table_oid=2606 dump_id=5337
ALTER TABLE ONLY public.corrective_action_statuses
    ADD CONSTRAINT corrective_action_statuses_code_key UNIQUE (code);

-- desc=CONSTRAINT tag='corrective_action_statuses corrective_action_statuses_pkey' namespace='public' oid=27060 table_oid=2606 dump_id=5339
ALTER TABLE ONLY public.corrective_action_statuses
    ADD CONSTRAINT corrective_action_statuses_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='corrective_actions corrective_actions_pkey' namespace='public' oid=27813 table_oid=2606 dump_id=5413
ALTER TABLE ONLY public.corrective_actions
    ADD CONSTRAINT corrective_actions_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='criteria criteria_pkey' namespace='public' oid=27364 table_oid=2606 dump_id=5369
ALTER TABLE ONLY public.criteria
    ADD CONSTRAINT criteria_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='criteria uq_criteria_section_code' namespace='public' oid=27366 table_oid=2606 dump_id=5372
ALTER TABLE ONLY public.criteria
    ADD CONSTRAINT uq_criteria_section_code UNIQUE (checklist_section_id, code);

-- desc=CONSTRAINT tag='finding_followups finding_followups_pkey' namespace='public' oid=27863 table_oid=2606 dump_id=5417
ALTER TABLE ONLY public.finding_followups
    ADD CONSTRAINT finding_followups_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='finding_priorities finding_priorities_code_key' namespace='public' oid=27032 table_oid=2606 dump_id=5329
ALTER TABLE ONLY public.finding_priorities
    ADD CONSTRAINT finding_priorities_code_key UNIQUE (code);

-- desc=CONSTRAINT tag='finding_priorities finding_priorities_pkey' namespace='public' oid=27030 table_oid=2606 dump_id=5331
ALTER TABLE ONLY public.finding_priorities
    ADD CONSTRAINT finding_priorities_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='finding_statuses finding_statuses_code_key' namespace='public' oid=27047 table_oid=2606 dump_id=5333
ALTER TABLE ONLY public.finding_statuses
    ADD CONSTRAINT finding_statuses_code_key UNIQUE (code);

-- desc=CONSTRAINT tag='finding_statuses finding_statuses_pkey' namespace='public' oid=27045 table_oid=2606 dump_id=5335
ALTER TABLE ONLY public.finding_statuses
    ADD CONSTRAINT finding_statuses_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='finding_types finding_types_code_key' namespace='public' oid=27019 table_oid=2606 dump_id=5325
ALTER TABLE ONLY public.finding_types
    ADD CONSTRAINT finding_types_code_key UNIQUE (code);

-- desc=CONSTRAINT tag='finding_types finding_types_pkey' namespace='public' oid=27017 table_oid=2606 dump_id=5327
ALTER TABLE ONLY public.finding_types
    ADD CONSTRAINT finding_types_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='findings findings_pkey' namespace='public' oid=27738 table_oid=2606 dump_id=5407
ALTER TABLE ONLY public.findings
    ADD CONSTRAINT findings_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='findings uq_findings_audit_number' namespace='public' oid=27740 table_oid=2606 dump_id=5411
ALTER TABLE ONLY public.findings
    ADD CONSTRAINT uq_findings_audit_number UNIQUE (audit_id, finding_number);

-- desc=CONSTRAINT tag='idempotency_operations idempotency_operations_pkey' namespace='public' oid=28155 table_oid=2606 dump_id=5444
ALTER TABLE ONLY public.idempotency_operations
    ADD CONSTRAINT idempotency_operations_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='idempotency_operations uq_idempotency_operation' namespace='public' oid=28157 table_oid=2606 dump_id=5447
ALTER TABLE ONLY public.idempotency_operations
    ADD CONSTRAINT uq_idempotency_operation UNIQUE (tenant_id, operation_id);

-- desc=CONSTRAINT tag='notifications notifications_pkey' namespace='public' oid=28127 table_oid=2606 dump_id=5442
ALTER TABLE ONLY public.notifications
    ADD CONSTRAINT notifications_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='observations observations_pkey' namespace='public' oid=27606 table_oid=2606 dump_id=5398
ALTER TABLE ONLY public.observations
    ADD CONSTRAINT observations_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='outbox_messages outbox_messages_pkey' namespace='public' oid=28189 table_oid=2606 dump_id=5450
ALTER TABLE ONLY public.outbox_messages
    ADD CONSTRAINT outbox_messages_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='password_reset_tokens password_reset_tokens_pkey' namespace='public' oid=26894 table_oid=2606 dump_id=5299
ALTER TABLE ONLY public.password_reset_tokens
    ADD CONSTRAINT password_reset_tokens_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='permissions permissions_code_key' namespace='public' oid=26799 table_oid=2606 dump_id=5285
ALTER TABLE ONLY public.permissions
    ADD CONSTRAINT permissions_code_key UNIQUE (code);

-- desc=CONSTRAINT tag='permissions permissions_pkey' namespace='public' oid=26797 table_oid=2606 dump_id=5287
ALTER TABLE ONLY public.permissions
    ADD CONSTRAINT permissions_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='profiles profiles_code_key' namespace='public' oid=26961 table_oid=2606 dump_id=5309
ALTER TABLE ONLY public.profiles
    ADD CONSTRAINT profiles_code_key UNIQUE (code);

-- desc=CONSTRAINT tag='profiles profiles_pkey' namespace='public' oid=26959 table_oid=2606 dump_id=5311
ALTER TABLE ONLY public.profiles
    ADD CONSTRAINT profiles_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='programs programs_code_key' namespace='public' oid=26946 table_oid=2606 dump_id=5305
ALTER TABLE ONLY public.programs
    ADD CONSTRAINT programs_code_key UNIQUE (code);

-- desc=CONSTRAINT tag='programs programs_pkey' namespace='public' oid=26944 table_oid=2606 dump_id=5307
ALTER TABLE ONLY public.programs
    ADD CONSTRAINT programs_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='refresh_tokens refresh_tokens_pkey' namespace='public' oid=26919 table_oid=2606 dump_id=5301
ALTER TABLE ONLY public.refresh_tokens
    ADD CONSTRAINT refresh_tokens_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='refresh_tokens refresh_tokens_token_hash_key' namespace='public' oid=26921 table_oid=2606 dump_id=5303
ALTER TABLE ONLY public.refresh_tokens
    ADD CONSTRAINT refresh_tokens_token_hash_key UNIQUE (token_hash);

-- desc=CONSTRAINT tag='report_template_versions report_template_versions_pkey' namespace='public' oid=27996 table_oid=2606 dump_id=5430
ALTER TABLE ONLY public.report_template_versions
    ADD CONSTRAINT report_template_versions_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='report_template_versions uq_report_template_version' namespace='public' oid=27998 table_oid=2606 dump_id=5432
ALTER TABLE ONLY public.report_template_versions
    ADD CONSTRAINT uq_report_template_version UNIQUE (report_template_id, version_number);

-- desc=CONSTRAINT tag='report_templates report_templates_pkey' namespace='public' oid=27962 table_oid=2606 dump_id=5425
ALTER TABLE ONLY public.report_templates
    ADD CONSTRAINT report_templates_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='report_templates uq_report_templates_tenant_code' namespace='public' oid=27964 table_oid=2606 dump_id=5427
ALTER TABLE ONLY public.report_templates
    ADD CONSTRAINT uq_report_templates_tenant_code UNIQUE (tenant_id, code);

-- desc=CONSTRAINT tag='role_permissions pk_role_permissions' namespace='public' oid=26806 table_oid=2606 dump_id=5289
ALTER TABLE ONLY public.role_permissions
    ADD CONSTRAINT pk_role_permissions PRIMARY KEY (role_id, permission_id);

-- desc=CONSTRAINT tag='roles roles_code_key' namespace='public' oid=26786 table_oid=2606 dump_id=5281
ALTER TABLE ONLY public.roles
    ADD CONSTRAINT roles_code_key UNIQUE (code);

-- desc=CONSTRAINT tag='roles roles_pkey' namespace='public' oid=26784 table_oid=2606 dump_id=5283
ALTER TABLE ONLY public.roles
    ADD CONSTRAINT roles_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='tenants tenants_pkey' namespace='public' oid=26771 table_oid=2606 dump_id=5277
ALTER TABLE ONLY public.tenants
    ADD CONSTRAINT tenants_pkey PRIMARY KEY (id);

-- desc=CONSTRAINT tag='tenants tenants_slug_key' namespace='public' oid=26773 table_oid=2606 dump_id=5279
ALTER TABLE ONLY public.tenants
    ADD CONSTRAINT tenants_slug_key UNIQUE (slug);

-- desc=CONSTRAINT tag='user_client_access pk_user_client_access' namespace='public' oid=27219 table_oid=2606 dump_id=5357
ALTER TABLE ONLY public.user_client_access
    ADD CONSTRAINT pk_user_client_access PRIMARY KEY (user_id, client_id);

-- desc=CONSTRAINT tag='user_roles pk_user_roles' namespace='public' oid=26859 table_oid=2606 dump_id=5297
ALTER TABLE ONLY public.user_roles
    ADD CONSTRAINT pk_user_roles PRIMARY KEY (user_id, role_id);

-- desc=CONSTRAINT tag='users uq_users_tenant_normalized_email' namespace='public' oid=26844 table_oid=2606 dump_id=5293
ALTER TABLE ONLY public.users
    ADD CONSTRAINT uq_users_tenant_normalized_email UNIQUE (tenant_id, normalized_email);

-- desc=CONSTRAINT tag='users users_pkey' namespace='public' oid=26842 table_oid=2606 dump_id=5295
ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (id);

-- ---- Constraints FK (126) ----
-- desc=FK CONSTRAINT tag='access_logs fk_access_logs_tenant' namespace='public' oid=28238 table_oid=2606 dump_id=5582
ALTER TABLE ONLY public.access_logs
    ADD CONSTRAINT fk_access_logs_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='access_logs fk_access_logs_user' namespace='public' oid=28243 table_oid=2606 dump_id=5583
ALTER TABLE ONLY public.access_logs
    ADD CONSTRAINT fk_access_logs_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audit_checklists fk_audit_checklists_audit' namespace='public' oid=27490 table_oid=2606 dump_id=5509
ALTER TABLE ONLY public.audit_checklists
    ADD CONSTRAINT fk_audit_checklists_audit FOREIGN KEY (audit_id) REFERENCES public.audits(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_checklists fk_audit_checklists_tenant' namespace='public' oid=27485 table_oid=2606 dump_id=5510
ALTER TABLE ONLY public.audit_checklists
    ADD CONSTRAINT fk_audit_checklists_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_checklists fk_audit_checklists_version' namespace='public' oid=27495 table_oid=2606 dump_id=5511
ALTER TABLE ONLY public.audit_checklists
    ADD CONSTRAINT fk_audit_checklists_version FOREIGN KEY (checklist_version_id) REFERENCES public.checklist_versions(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audit_criteria fk_audit_criteria_audit' namespace='public' oid=27563 table_oid=2606 dump_id=5516
ALTER TABLE ONLY public.audit_criteria
    ADD CONSTRAINT fk_audit_criteria_audit FOREIGN KEY (audit_id) REFERENCES public.audits(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_criteria fk_audit_criteria_checklist' namespace='public' oid=27568 table_oid=2606 dump_id=5517
ALTER TABLE ONLY public.audit_criteria
    ADD CONSTRAINT fk_audit_criteria_checklist FOREIGN KEY (audit_checklist_id) REFERENCES public.audit_checklists(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_criteria fk_audit_criteria_compliance' namespace='public' oid=27578 table_oid=2606 dump_id=5518
ALTER TABLE ONLY public.audit_criteria
    ADD CONSTRAINT fk_audit_criteria_compliance FOREIGN KEY (compliance_status_id) REFERENCES public.compliance_statuses(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audit_criteria fk_audit_criteria_criterion' namespace='public' oid=27573 table_oid=2606 dump_id=5519
ALTER TABLE ONLY public.audit_criteria
    ADD CONSTRAINT fk_audit_criteria_criterion FOREIGN KEY (criterion_id) REFERENCES public.criteria(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audit_criteria fk_audit_criteria_evaluated_by' namespace='public' oid=27583 table_oid=2606 dump_id=5520
ALTER TABLE ONLY public.audit_criteria
    ADD CONSTRAINT fk_audit_criteria_evaluated_by FOREIGN KEY (evaluated_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audit_criteria fk_audit_criteria_tenant' namespace='public' oid=27558 table_oid=2606 dump_id=5521
ALTER TABLE ONLY public.audit_criteria
    ADD CONSTRAINT fk_audit_criteria_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_document_requests fk_document_requests_audit' namespace='public' oid=27652 table_oid=2606 dump_id=5526
ALTER TABLE ONLY public.audit_document_requests
    ADD CONSTRAINT fk_document_requests_audit FOREIGN KEY (audit_id) REFERENCES public.audits(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_document_requests fk_document_requests_requested_by' namespace='public' oid=27657 table_oid=2606 dump_id=5527
ALTER TABLE ONLY public.audit_document_requests
    ADD CONSTRAINT fk_document_requests_requested_by FOREIGN KEY (requested_by_user_id) REFERENCES public.users(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audit_document_requests fk_document_requests_tenant' namespace='public' oid=27647 table_oid=2606 dump_id=5528
ALTER TABLE ONLY public.audit_document_requests
    ADD CONSTRAINT fk_document_requests_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_evidences fk_evidence_audit' namespace='public' oid=27693 table_oid=2606 dump_id=5529
ALTER TABLE ONLY public.audit_evidences
    ADD CONSTRAINT fk_evidence_audit FOREIGN KEY (audit_id) REFERENCES public.audits(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_evidences fk_evidence_corrective_action' namespace='public' oid=27889 table_oid=2606 dump_id=5530
ALTER TABLE ONLY public.audit_evidences
    ADD CONSTRAINT fk_evidence_corrective_action FOREIGN KEY (corrective_action_id) REFERENCES public.corrective_actions(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audit_evidences fk_evidence_criterion' namespace='public' oid=27698 table_oid=2606 dump_id=5531
ALTER TABLE ONLY public.audit_evidences
    ADD CONSTRAINT fk_evidence_criterion FOREIGN KEY (audit_criterion_id) REFERENCES public.audit_criteria(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audit_evidences fk_evidence_document_request' namespace='public' oid=27708 table_oid=2606 dump_id=5532
ALTER TABLE ONLY public.audit_evidences
    ADD CONSTRAINT fk_evidence_document_request FOREIGN KEY (document_request_id) REFERENCES public.audit_document_requests(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audit_evidences fk_evidence_finding' namespace='public' oid=27884 table_oid=2606 dump_id=5533
ALTER TABLE ONLY public.audit_evidences
    ADD CONSTRAINT fk_evidence_finding FOREIGN KEY (finding_id) REFERENCES public.findings(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audit_evidences fk_evidence_tenant' namespace='public' oid=27688 table_oid=2606 dump_id=5534
ALTER TABLE ONLY public.audit_evidences
    ADD CONSTRAINT fk_evidence_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_evidences fk_evidence_uploader' namespace='public' oid=27703 table_oid=2606 dump_id=5535
ALTER TABLE ONLY public.audit_evidences
    ADD CONSTRAINT fk_evidence_uploader FOREIGN KEY (uploaded_by_user_id) REFERENCES public.users(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audit_logs fk_audit_logs_audit' namespace='public' oid=28219 table_oid=2606 dump_id=5579
ALTER TABLE ONLY public.audit_logs
    ADD CONSTRAINT fk_audit_logs_audit FOREIGN KEY (audit_id) REFERENCES public.audits(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audit_logs fk_audit_logs_tenant' namespace='public' oid=28209 table_oid=2606 dump_id=5580
ALTER TABLE ONLY public.audit_logs
    ADD CONSTRAINT fk_audit_logs_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audit_logs fk_audit_logs_user' namespace='public' oid=28214 table_oid=2606 dump_id=5581
ALTER TABLE ONLY public.audit_logs
    ADD CONSTRAINT fk_audit_logs_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audit_programs fk_audit_programs_audit' namespace='public' oid=27461 table_oid=2606 dump_id=5506
ALTER TABLE ONLY public.audit_programs
    ADD CONSTRAINT fk_audit_programs_audit FOREIGN KEY (audit_id) REFERENCES public.audits(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_programs fk_audit_programs_program' namespace='public' oid=27466 table_oid=2606 dump_id=5507
ALTER TABLE ONLY public.audit_programs
    ADD CONSTRAINT fk_audit_programs_program FOREIGN KEY (program_id) REFERENCES public.programs(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audit_programs fk_audit_programs_tenant' namespace='public' oid=27456 table_oid=2606 dump_id=5508
ALTER TABLE ONLY public.audit_programs
    ADD CONSTRAINT fk_audit_programs_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_reports fk_audit_reports_audit' namespace='public' oid=28046 table_oid=2606 dump_id=5565
ALTER TABLE ONLY public.audit_reports
    ADD CONSTRAINT fk_audit_reports_audit FOREIGN KEY (audit_id) REFERENCES public.audits(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_reports fk_audit_reports_generated_by' namespace='public' oid=28056 table_oid=2606 dump_id=5566
ALTER TABLE ONLY public.audit_reports
    ADD CONSTRAINT fk_audit_reports_generated_by FOREIGN KEY (generated_by_user_id) REFERENCES public.users(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audit_reports fk_audit_reports_template_version' namespace='public' oid=28051 table_oid=2606 dump_id=5567
ALTER TABLE ONLY public.audit_reports
    ADD CONSTRAINT fk_audit_reports_template_version FOREIGN KEY (report_template_version_id) REFERENCES public.report_template_versions(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audit_reports fk_audit_reports_tenant' namespace='public' oid=28041 table_oid=2606 dump_id=5568
ALTER TABLE ONLY public.audit_reports
    ADD CONSTRAINT fk_audit_reports_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_reports fk_audit_reports_validated_by' namespace='public' oid=28061 table_oid=2606 dump_id=5569
ALTER TABLE ONLY public.audit_reports
    ADD CONSTRAINT fk_audit_reports_validated_by FOREIGN KEY (validated_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audit_results fk_audit_results_audit' namespace='public' oid=27932 table_oid=2606 dump_id=5557
ALTER TABLE ONLY public.audit_results
    ADD CONSTRAINT fk_audit_results_audit FOREIGN KEY (audit_id) REFERENCES public.audits(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_results fk_audit_results_finalized_by' namespace='public' oid=27937 table_oid=2606 dump_id=5558
ALTER TABLE ONLY public.audit_results
    ADD CONSTRAINT fk_audit_results_finalized_by FOREIGN KEY (finalized_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audit_results fk_audit_results_tenant' namespace='public' oid=27927 table_oid=2606 dump_id=5559
ALTER TABLE ONLY public.audit_results
    ADD CONSTRAINT fk_audit_results_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_signatories fk_audit_signatories_audit' namespace='public' oid=28092 table_oid=2606 dump_id=5570
ALTER TABLE ONLY public.audit_signatories
    ADD CONSTRAINT fk_audit_signatories_audit FOREIGN KEY (audit_id) REFERENCES public.audits(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_signatories fk_audit_signatories_contact' namespace='public' oid=28102 table_oid=2606 dump_id=5571
ALTER TABLE ONLY public.audit_signatories
    ADD CONSTRAINT fk_audit_signatories_contact FOREIGN KEY (client_contact_id) REFERENCES public.client_contacts(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audit_signatories fk_audit_signatories_tenant' namespace='public' oid=28087 table_oid=2606 dump_id=5572
ALTER TABLE ONLY public.audit_signatories
    ADD CONSTRAINT fk_audit_signatories_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_signatories fk_audit_signatories_user' namespace='public' oid=28097 table_oid=2606 dump_id=5573
ALTER TABLE ONLY public.audit_signatories
    ADD CONSTRAINT fk_audit_signatories_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audit_team fk_audit_team_assigned_by' namespace='public' oid=27527 table_oid=2606 dump_id=5512
ALTER TABLE ONLY public.audit_team
    ADD CONSTRAINT fk_audit_team_assigned_by FOREIGN KEY (assigned_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audit_team fk_audit_team_audit' namespace='public' oid=27517 table_oid=2606 dump_id=5513
ALTER TABLE ONLY public.audit_team
    ADD CONSTRAINT fk_audit_team_audit FOREIGN KEY (audit_id) REFERENCES public.audits(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_team fk_audit_team_tenant' namespace='public' oid=27512 table_oid=2606 dump_id=5514
ALTER TABLE ONLY public.audit_team
    ADD CONSTRAINT fk_audit_team_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='audit_team fk_audit_team_user' namespace='public' oid=27522 table_oid=2606 dump_id=5515
ALTER TABLE ONLY public.audit_team
    ADD CONSTRAINT fk_audit_team_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audited_companies fk_audited_companies_client' namespace='public' oid=27134 table_oid=2606 dump_id=5473
ALTER TABLE ONLY public.audited_companies
    ADD CONSTRAINT fk_audited_companies_client FOREIGN KEY (client_id) REFERENCES public.clients(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audited_companies fk_audited_companies_profile' namespace='public' oid=27139 table_oid=2606 dump_id=5474
ALTER TABLE ONLY public.audited_companies
    ADD CONSTRAINT fk_audited_companies_profile FOREIGN KEY (default_profile_id) REFERENCES public.profiles(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audited_companies fk_audited_companies_tenant' namespace='public' oid=27129 table_oid=2606 dump_id=5475
ALTER TABLE ONLY public.audited_companies
    ADD CONSTRAINT fk_audited_companies_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audits fk_audits_client' namespace='public' oid=27408 table_oid=2606 dump_id=5497
ALTER TABLE ONLY public.audits
    ADD CONSTRAINT fk_audits_client FOREIGN KEY (client_id) REFERENCES public.clients(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audits fk_audits_company' namespace='public' oid=27413 table_oid=2606 dump_id=5498
ALTER TABLE ONLY public.audits
    ADD CONSTRAINT fk_audits_company FOREIGN KEY (audited_company_id) REFERENCES public.audited_companies(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audits fk_audits_created_by' namespace='public' oid=27438 table_oid=2606 dump_id=5499
ALTER TABLE ONLY public.audits
    ADD CONSTRAINT fk_audits_created_by FOREIGN KEY (created_by_user_id) REFERENCES public.users(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audits fk_audits_profile' namespace='public' oid=27428 table_oid=2606 dump_id=5500
ALTER TABLE ONLY public.audits
    ADD CONSTRAINT fk_audits_profile FOREIGN KEY (profile_id) REFERENCES public.profiles(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audits fk_audits_site' namespace='public' oid=27418 table_oid=2606 dump_id=5501
ALTER TABLE ONLY public.audits
    ADD CONSTRAINT fk_audits_site FOREIGN KEY (company_site_id) REFERENCES public.company_sites(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='audits fk_audits_status' namespace='public' oid=27433 table_oid=2606 dump_id=5502
ALTER TABLE ONLY public.audits
    ADD CONSTRAINT fk_audits_status FOREIGN KEY (status_id) REFERENCES public.audit_statuses(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audits fk_audits_tenant' namespace='public' oid=27403 table_oid=2606 dump_id=5503
ALTER TABLE ONLY public.audits
    ADD CONSTRAINT fk_audits_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audits fk_audits_type' namespace='public' oid=27423 table_oid=2606 dump_id=5504
ALTER TABLE ONLY public.audits
    ADD CONSTRAINT fk_audits_type FOREIGN KEY (audit_type_id) REFERENCES public.audit_types(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='audits fk_audits_validated_by' namespace='public' oid=27443 table_oid=2606 dump_id=5505
ALTER TABLE ONLY public.audits
    ADD CONSTRAINT fk_audits_validated_by FOREIGN KEY (validated_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='checklist_sections fk_checklist_sections_tenant' namespace='public' oid=27337 table_oid=2606 dump_id=5493
ALTER TABLE ONLY public.checklist_sections
    ADD CONSTRAINT fk_checklist_sections_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='checklist_sections fk_checklist_sections_version' namespace='public' oid=27342 table_oid=2606 dump_id=5494
ALTER TABLE ONLY public.checklist_sections
    ADD CONSTRAINT fk_checklist_sections_version FOREIGN KEY (checklist_version_id) REFERENCES public.checklist_versions(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='checklist_versions fk_checklist_versions_checklist' namespace='public' oid=27313 table_oid=2606 dump_id=5490
ALTER TABLE ONLY public.checklist_versions
    ADD CONSTRAINT fk_checklist_versions_checklist FOREIGN KEY (checklist_id) REFERENCES public.checklists(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='checklist_versions fk_checklist_versions_created_by' namespace='public' oid=27318 table_oid=2606 dump_id=5491
ALTER TABLE ONLY public.checklist_versions
    ADD CONSTRAINT fk_checklist_versions_created_by FOREIGN KEY (created_by_user_id) REFERENCES public.users(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='checklist_versions fk_checklist_versions_tenant' namespace='public' oid=27308 table_oid=2606 dump_id=5492
ALTER TABLE ONLY public.checklist_versions
    ADD CONSTRAINT fk_checklist_versions_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='checklists fk_checklists_audit_type' namespace='public' oid=27275 table_oid=2606 dump_id=5485
ALTER TABLE ONLY public.checklists
    ADD CONSTRAINT fk_checklists_audit_type FOREIGN KEY (audit_type_id) REFERENCES public.audit_types(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='checklists fk_checklists_created_by' namespace='public' oid=27280 table_oid=2606 dump_id=5486
ALTER TABLE ONLY public.checklists
    ADD CONSTRAINT fk_checklists_created_by FOREIGN KEY (created_by_user_id) REFERENCES public.users(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='checklists fk_checklists_profile' namespace='public' oid=27270 table_oid=2606 dump_id=5487
ALTER TABLE ONLY public.checklists
    ADD CONSTRAINT fk_checklists_profile FOREIGN KEY (profile_id) REFERENCES public.profiles(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='checklists fk_checklists_program' namespace='public' oid=27265 table_oid=2606 dump_id=5488
ALTER TABLE ONLY public.checklists
    ADD CONSTRAINT fk_checklists_program FOREIGN KEY (program_id) REFERENCES public.programs(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='checklists fk_checklists_tenant' namespace='public' oid=27260 table_oid=2606 dump_id=5489
ALTER TABLE ONLY public.checklists
    ADD CONSTRAINT fk_checklists_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='client_contacts fk_client_contacts_client' namespace='public' oid=27200 table_oid=2606 dump_id=5478
ALTER TABLE ONLY public.client_contacts
    ADD CONSTRAINT fk_client_contacts_client FOREIGN KEY (client_id) REFERENCES public.clients(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='client_contacts fk_client_contacts_company' namespace='public' oid=27205 table_oid=2606 dump_id=5479
ALTER TABLE ONLY public.client_contacts
    ADD CONSTRAINT fk_client_contacts_company FOREIGN KEY (audited_company_id) REFERENCES public.audited_companies(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='client_contacts fk_client_contacts_tenant' namespace='public' oid=27195 table_oid=2606 dump_id=5480
ALTER TABLE ONLY public.client_contacts
    ADD CONSTRAINT fk_client_contacts_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='client_programs fk_client_programs_client' namespace='public' oid=27099 table_oid=2606 dump_id=5470
ALTER TABLE ONLY public.client_programs
    ADD CONSTRAINT fk_client_programs_client FOREIGN KEY (client_id) REFERENCES public.clients(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='client_programs fk_client_programs_program' namespace='public' oid=27104 table_oid=2606 dump_id=5471
ALTER TABLE ONLY public.client_programs
    ADD CONSTRAINT fk_client_programs_program FOREIGN KEY (program_id) REFERENCES public.programs(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='client_programs fk_client_programs_tenant' namespace='public' oid=27094 table_oid=2606 dump_id=5472
ALTER TABLE ONLY public.client_programs
    ADD CONSTRAINT fk_client_programs_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='clients fk_clients_tenant' namespace='public' oid=27080 table_oid=2606 dump_id=5469
ALTER TABLE ONLY public.clients
    ADD CONSTRAINT fk_clients_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='company_sites fk_company_sites_company' namespace='public' oid=27171 table_oid=2606 dump_id=5476
ALTER TABLE ONLY public.company_sites
    ADD CONSTRAINT fk_company_sites_company FOREIGN KEY (audited_company_id) REFERENCES public.audited_companies(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='company_sites fk_company_sites_tenant' namespace='public' oid=27166 table_oid=2606 dump_id=5477
ALTER TABLE ONLY public.company_sites
    ADD CONSTRAINT fk_company_sites_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='corrective_actions fk_corrective_actions_created_by' namespace='public' oid=27844 table_oid=2606 dump_id=5546
ALTER TABLE ONLY public.corrective_actions
    ADD CONSTRAINT fk_corrective_actions_created_by FOREIGN KEY (created_by_user_id) REFERENCES public.users(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='corrective_actions fk_corrective_actions_finding' namespace='public' oid=27819 table_oid=2606 dump_id=5547
ALTER TABLE ONLY public.corrective_actions
    ADD CONSTRAINT fk_corrective_actions_finding FOREIGN KEY (finding_id) REFERENCES public.findings(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='corrective_actions fk_corrective_actions_responsible_contact' namespace='public' oid=27834 table_oid=2606 dump_id=5548
ALTER TABLE ONLY public.corrective_actions
    ADD CONSTRAINT fk_corrective_actions_responsible_contact FOREIGN KEY (responsible_contact_id) REFERENCES public.client_contacts(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='corrective_actions fk_corrective_actions_responsible_user' namespace='public' oid=27829 table_oid=2606 dump_id=5549
ALTER TABLE ONLY public.corrective_actions
    ADD CONSTRAINT fk_corrective_actions_responsible_user FOREIGN KEY (responsible_user_id) REFERENCES public.users(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='corrective_actions fk_corrective_actions_status' namespace='public' oid=27824 table_oid=2606 dump_id=5550
ALTER TABLE ONLY public.corrective_actions
    ADD CONSTRAINT fk_corrective_actions_status FOREIGN KEY (status_id) REFERENCES public.corrective_action_statuses(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='corrective_actions fk_corrective_actions_tenant' namespace='public' oid=27814 table_oid=2606 dump_id=5551
ALTER TABLE ONLY public.corrective_actions
    ADD CONSTRAINT fk_corrective_actions_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='corrective_actions fk_corrective_actions_validated_by' namespace='public' oid=27839 table_oid=2606 dump_id=5552
ALTER TABLE ONLY public.corrective_actions
    ADD CONSTRAINT fk_corrective_actions_validated_by FOREIGN KEY (validated_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='criteria fk_criteria_section' namespace='public' oid=27372 table_oid=2606 dump_id=5495
ALTER TABLE ONLY public.criteria
    ADD CONSTRAINT fk_criteria_section FOREIGN KEY (checklist_section_id) REFERENCES public.checklist_sections(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='criteria fk_criteria_tenant' namespace='public' oid=27367 table_oid=2606 dump_id=5496
ALTER TABLE ONLY public.criteria
    ADD CONSTRAINT fk_criteria_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='finding_followups fk_finding_followups_action' namespace='public' oid=27874 table_oid=2606 dump_id=5553
ALTER TABLE ONLY public.finding_followups
    ADD CONSTRAINT fk_finding_followups_action FOREIGN KEY (corrective_action_id) REFERENCES public.corrective_actions(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='finding_followups fk_finding_followups_author' namespace='public' oid=27879 table_oid=2606 dump_id=5554
ALTER TABLE ONLY public.finding_followups
    ADD CONSTRAINT fk_finding_followups_author FOREIGN KEY (author_user_id) REFERENCES public.users(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='finding_followups fk_finding_followups_finding' namespace='public' oid=27869 table_oid=2606 dump_id=5555
ALTER TABLE ONLY public.finding_followups
    ADD CONSTRAINT fk_finding_followups_finding FOREIGN KEY (finding_id) REFERENCES public.findings(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='finding_followups fk_finding_followups_tenant' namespace='public' oid=27864 table_oid=2606 dump_id=5556
ALTER TABLE ONLY public.finding_followups
    ADD CONSTRAINT fk_finding_followups_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='findings fk_findings_audit' namespace='public' oid=27746 table_oid=2606 dump_id=5536
ALTER TABLE ONLY public.findings
    ADD CONSTRAINT fk_findings_audit FOREIGN KEY (audit_id) REFERENCES public.audits(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='findings fk_findings_created_by' namespace='public' oid=27781 table_oid=2606 dump_id=5537
ALTER TABLE ONLY public.findings
    ADD CONSTRAINT fk_findings_created_by FOREIGN KEY (created_by_user_id) REFERENCES public.users(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='findings fk_findings_criterion' namespace='public' oid=27751 table_oid=2606 dump_id=5538
ALTER TABLE ONLY public.findings
    ADD CONSTRAINT fk_findings_criterion FOREIGN KEY (audit_criterion_id) REFERENCES public.audit_criteria(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='findings fk_findings_priority' namespace='public' oid=27761 table_oid=2606 dump_id=5539
ALTER TABLE ONLY public.findings
    ADD CONSTRAINT fk_findings_priority FOREIGN KEY (priority_id) REFERENCES public.finding_priorities(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='findings fk_findings_responsible_contact' namespace='public' oid=27776 table_oid=2606 dump_id=5540
ALTER TABLE ONLY public.findings
    ADD CONSTRAINT fk_findings_responsible_contact FOREIGN KEY (responsible_contact_id) REFERENCES public.client_contacts(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='findings fk_findings_responsible_user' namespace='public' oid=27771 table_oid=2606 dump_id=5541
ALTER TABLE ONLY public.findings
    ADD CONSTRAINT fk_findings_responsible_user FOREIGN KEY (responsible_user_id) REFERENCES public.users(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='findings fk_findings_status' namespace='public' oid=27766 table_oid=2606 dump_id=5542
ALTER TABLE ONLY public.findings
    ADD CONSTRAINT fk_findings_status FOREIGN KEY (status_id) REFERENCES public.finding_statuses(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='findings fk_findings_tenant' namespace='public' oid=27741 table_oid=2606 dump_id=5543
ALTER TABLE ONLY public.findings
    ADD CONSTRAINT fk_findings_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='findings fk_findings_type' namespace='public' oid=27756 table_oid=2606 dump_id=5544
ALTER TABLE ONLY public.findings
    ADD CONSTRAINT fk_findings_type FOREIGN KEY (finding_type_id) REFERENCES public.finding_types(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='findings fk_findings_validated_by' namespace='public' oid=27786 table_oid=2606 dump_id=5545
ALTER TABLE ONLY public.findings
    ADD CONSTRAINT fk_findings_validated_by FOREIGN KEY (validated_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='idempotency_operations fk_idempotency_tenant' namespace='public' oid=28158 table_oid=2606 dump_id=5576
ALTER TABLE ONLY public.idempotency_operations
    ADD CONSTRAINT fk_idempotency_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='idempotency_operations fk_idempotency_user' namespace='public' oid=28163 table_oid=2606 dump_id=5577
ALTER TABLE ONLY public.idempotency_operations
    ADD CONSTRAINT fk_idempotency_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='notifications fk_notifications_tenant' namespace='public' oid=28128 table_oid=2606 dump_id=5574
ALTER TABLE ONLY public.notifications
    ADD CONSTRAINT fk_notifications_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='notifications fk_notifications_user' namespace='public' oid=28133 table_oid=2606 dump_id=5575
ALTER TABLE ONLY public.notifications
    ADD CONSTRAINT fk_notifications_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='observations fk_observations_audit' namespace='public' oid=27612 table_oid=2606 dump_id=5522
ALTER TABLE ONLY public.observations
    ADD CONSTRAINT fk_observations_audit FOREIGN KEY (audit_id) REFERENCES public.audits(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='observations fk_observations_author' namespace='public' oid=27622 table_oid=2606 dump_id=5523
ALTER TABLE ONLY public.observations
    ADD CONSTRAINT fk_observations_author FOREIGN KEY (author_user_id) REFERENCES public.users(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='observations fk_observations_criterion' namespace='public' oid=27617 table_oid=2606 dump_id=5524
ALTER TABLE ONLY public.observations
    ADD CONSTRAINT fk_observations_criterion FOREIGN KEY (audit_criterion_id) REFERENCES public.audit_criteria(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='observations fk_observations_tenant' namespace='public' oid=27607 table_oid=2606 dump_id=5525
ALTER TABLE ONLY public.observations
    ADD CONSTRAINT fk_observations_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='outbox_messages fk_outbox_tenant' namespace='public' oid=28190 table_oid=2606 dump_id=5578
ALTER TABLE ONLY public.outbox_messages
    ADD CONSTRAINT fk_outbox_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='password_reset_tokens fk_password_reset_tenant' namespace='public' oid=26895 table_oid=2606 dump_id=5465
ALTER TABLE ONLY public.password_reset_tokens
    ADD CONSTRAINT fk_password_reset_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='password_reset_tokens fk_password_reset_user' namespace='public' oid=26900 table_oid=2606 dump_id=5466
ALTER TABLE ONLY public.password_reset_tokens
    ADD CONSTRAINT fk_password_reset_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='refresh_tokens fk_refresh_tokens_tenant' namespace='public' oid=26922 table_oid=2606 dump_id=5467
ALTER TABLE ONLY public.refresh_tokens
    ADD CONSTRAINT fk_refresh_tokens_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='refresh_tokens fk_refresh_tokens_user' namespace='public' oid=26927 table_oid=2606 dump_id=5468
ALTER TABLE ONLY public.refresh_tokens
    ADD CONSTRAINT fk_refresh_tokens_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='report_template_versions fk_report_template_versions_created_by' namespace='public' oid=28009 table_oid=2606 dump_id=5562
ALTER TABLE ONLY public.report_template_versions
    ADD CONSTRAINT fk_report_template_versions_created_by FOREIGN KEY (created_by_user_id) REFERENCES public.users(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='report_template_versions fk_report_template_versions_template' namespace='public' oid=28004 table_oid=2606 dump_id=5563
ALTER TABLE ONLY public.report_template_versions
    ADD CONSTRAINT fk_report_template_versions_template FOREIGN KEY (report_template_id) REFERENCES public.report_templates(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='report_template_versions fk_report_template_versions_tenant' namespace='public' oid=27999 table_oid=2606 dump_id=5564
ALTER TABLE ONLY public.report_template_versions
    ADD CONSTRAINT fk_report_template_versions_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='report_templates fk_report_templates_created_by' namespace='public' oid=27970 table_oid=2606 dump_id=5560
ALTER TABLE ONLY public.report_templates
    ADD CONSTRAINT fk_report_templates_created_by FOREIGN KEY (created_by_user_id) REFERENCES public.users(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='report_templates fk_report_templates_tenant' namespace='public' oid=27965 table_oid=2606 dump_id=5561
ALTER TABLE ONLY public.report_templates
    ADD CONSTRAINT fk_report_templates_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='role_permissions fk_role_permissions_permission' namespace='public' oid=26812 table_oid=2606 dump_id=5458
ALTER TABLE ONLY public.role_permissions
    ADD CONSTRAINT fk_role_permissions_permission FOREIGN KEY (permission_id) REFERENCES public.permissions(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='role_permissions fk_role_permissions_role' namespace='public' oid=26807 table_oid=2606 dump_id=5459
ALTER TABLE ONLY public.role_permissions
    ADD CONSTRAINT fk_role_permissions_role FOREIGN KEY (role_id) REFERENCES public.roles(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='user_client_access fk_user_client_access_client' namespace='public' oid=27230 table_oid=2606 dump_id=5481
ALTER TABLE ONLY public.user_client_access
    ADD CONSTRAINT fk_user_client_access_client FOREIGN KEY (client_id) REFERENCES public.clients(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='user_client_access fk_user_client_access_granted_by' namespace='public' oid=27235 table_oid=2606 dump_id=5482
ALTER TABLE ONLY public.user_client_access
    ADD CONSTRAINT fk_user_client_access_granted_by FOREIGN KEY (granted_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='user_client_access fk_user_client_access_tenant' namespace='public' oid=27220 table_oid=2606 dump_id=5483
ALTER TABLE ONLY public.user_client_access
    ADD CONSTRAINT fk_user_client_access_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='user_client_access fk_user_client_access_user' namespace='public' oid=27225 table_oid=2606 dump_id=5484
ALTER TABLE ONLY public.user_client_access
    ADD CONSTRAINT fk_user_client_access_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='user_roles fk_user_roles_assigned_by' namespace='public' oid=26875 table_oid=2606 dump_id=5461
ALTER TABLE ONLY public.user_roles
    ADD CONSTRAINT fk_user_roles_assigned_by FOREIGN KEY (assigned_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;

-- desc=FK CONSTRAINT tag='user_roles fk_user_roles_role' namespace='public' oid=26870 table_oid=2606 dump_id=5462
ALTER TABLE ONLY public.user_roles
    ADD CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) REFERENCES public.roles(id) ON DELETE RESTRICT;

-- desc=FK CONSTRAINT tag='user_roles fk_user_roles_tenant' namespace='public' oid=26860 table_oid=2606 dump_id=5463
ALTER TABLE ONLY public.user_roles
    ADD CONSTRAINT fk_user_roles_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='user_roles fk_user_roles_user' namespace='public' oid=26865 table_oid=2606 dump_id=5464
ALTER TABLE ONLY public.user_roles
    ADD CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;

-- desc=FK CONSTRAINT tag='users fk_users_tenant' namespace='public' oid=26845 table_oid=2606 dump_id=5460
ALTER TABLE ONLY public.users
    ADD CONSTRAINT fk_users_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE RESTRICT;

-- ---- Índices explícitos (36) ----
-- desc=INDEX tag='ix_access_logs_user' namespace='public' oid=28291 table_oid=1259 dump_id=5457
CREATE INDEX ix_access_logs_user ON public.access_logs USING btree (tenant_id, user_id, created_at_utc DESC);

-- desc=INDEX tag='ix_audit_criteria_audit' namespace='public' oid=28278 table_oid=1259 dump_id=5392
CREATE INDEX ix_audit_criteria_audit ON public.audit_criteria USING btree (tenant_id, audit_id);

-- desc=INDEX tag='ix_audit_criteria_pending' namespace='public' oid=28279 table_oid=1259 dump_id=5393
CREATE INDEX ix_audit_criteria_pending ON public.audit_criteria USING btree (tenant_id, audit_id) WHERE (compliance_status_id IS NULL);

-- desc=INDEX tag='ix_audit_logs_audit' namespace='public' oid=28290 table_oid=1259 dump_id=5453
CREATE INDEX ix_audit_logs_audit ON public.audit_logs USING btree (tenant_id, audit_id, created_at_utc DESC);

-- desc=INDEX tag='ix_audit_logs_entity' namespace='public' oid=28289 table_oid=1259 dump_id=5454
CREATE INDEX ix_audit_logs_entity ON public.audit_logs USING btree (tenant_id, entity_name, entity_id, created_at_utc DESC);

-- desc=INDEX tag='ix_audit_results_audit' namespace='public' oid=28292 table_oid=1259 dump_id=5422
CREATE INDEX ix_audit_results_audit ON public.audit_results USING btree (tenant_id, audit_id);

-- desc=INDEX tag='ix_audit_signatories_audit' namespace='public' oid=28295 table_oid=1259 dump_id=5439
CREATE INDEX ix_audit_signatories_audit ON public.audit_signatories USING btree (tenant_id, audit_id, sort_order);

-- desc=INDEX tag='ix_audit_team_user' namespace='public' oid=28277 table_oid=1259 dump_id=5386
CREATE INDEX ix_audit_team_user ON public.audit_team USING btree (tenant_id, user_id, audit_id);

-- desc=INDEX tag='ix_audited_companies_client' namespace='public' oid=28268 table_oid=1259 dump_id=5348
CREATE INDEX ix_audited_companies_client ON public.audited_companies USING btree (tenant_id, client_id);

-- desc=INDEX tag='ix_audits_client' namespace='public' oid=28274 table_oid=1259 dump_id=5375
CREATE INDEX ix_audits_client ON public.audits USING btree (tenant_id, client_id, scheduled_date DESC);

-- desc=INDEX tag='ix_audits_company' namespace='public' oid=28275 table_oid=1259 dump_id=5376
CREATE INDEX ix_audits_company ON public.audits USING btree (tenant_id, audited_company_id, scheduled_date DESC);

-- desc=INDEX tag='ix_audits_status' namespace='public' oid=28276 table_oid=1259 dump_id=5377
CREATE INDEX ix_audits_status ON public.audits USING btree (tenant_id, status_id, scheduled_date DESC);

-- desc=INDEX tag='ix_checklist_versions_checklist' namespace='public' oid=28272 table_oid=1259 dump_id=5363
CREATE INDEX ix_checklist_versions_checklist ON public.checklist_versions USING btree (tenant_id, checklist_id, status);

-- desc=INDEX tag='ix_checklists_selector' namespace='public' oid=28271 table_oid=1259 dump_id=5360
CREATE INDEX ix_checklists_selector ON public.checklists USING btree (tenant_id, program_id, profile_id, audit_type_id, is_active);

-- desc=INDEX tag='ix_client_contacts_client' namespace='public' oid=28270 table_oid=1259 dump_id=5355
CREATE INDEX ix_client_contacts_client ON public.client_contacts USING btree (tenant_id, client_id);

-- desc=INDEX tag='ix_clients_tenant' namespace='public' oid=28267 table_oid=1259 dump_id=5342
CREATE INDEX ix_clients_tenant ON public.clients USING btree (tenant_id);

-- desc=INDEX tag='ix_company_sites_company' namespace='public' oid=28269 table_oid=1259 dump_id=5352
CREATE INDEX ix_company_sites_company ON public.company_sites USING btree (tenant_id, audited_company_id);

-- desc=INDEX tag='ix_corrective_actions_commitment_date' namespace='public' oid=28287 table_oid=1259 dump_id=5414
CREATE INDEX ix_corrective_actions_commitment_date ON public.corrective_actions USING btree (tenant_id, commitment_date) WHERE (completed_at_utc IS NULL);

-- desc=INDEX tag='ix_corrective_actions_finding' namespace='public' oid=28286 table_oid=1259 dump_id=5415
CREATE INDEX ix_corrective_actions_finding ON public.corrective_actions USING btree (tenant_id, finding_id, status_id);

-- desc=INDEX tag='ix_criteria_section' namespace='public' oid=28273 table_oid=1259 dump_id=5370
CREATE INDEX ix_criteria_section ON public.criteria USING btree (tenant_id, checklist_section_id, sort_order);

-- desc=INDEX tag='ix_evidences_audit' namespace='public' oid=28281 table_oid=1259 dump_id=5403
CREATE INDEX ix_evidences_audit ON public.audit_evidences USING btree (tenant_id, audit_id, created_at_utc);

-- desc=INDEX tag='ix_evidences_criterion' namespace='public' oid=28282 table_oid=1259 dump_id=5404
CREATE INDEX ix_evidences_criterion ON public.audit_evidences USING btree (tenant_id, audit_criterion_id) WHERE (audit_criterion_id IS NOT NULL);

-- desc=INDEX tag='ix_evidences_finding' namespace='public' oid=28283 table_oid=1259 dump_id=5405
CREATE INDEX ix_evidences_finding ON public.audit_evidences USING btree (tenant_id, finding_id) WHERE (finding_id IS NOT NULL);

-- desc=INDEX tag='ix_findings_audit_status' namespace='public' oid=28284 table_oid=1259 dump_id=5408
CREATE INDEX ix_findings_audit_status ON public.findings USING btree (tenant_id, audit_id, status_id);

-- desc=INDEX tag='ix_findings_commitment_date' namespace='public' oid=28285 table_oid=1259 dump_id=5409
CREATE INDEX ix_findings_commitment_date ON public.findings USING btree (tenant_id, commitment_date) WHERE (closed_at_utc IS NULL);

-- desc=INDEX tag='ix_idempotency_operations_lookup' namespace='public' oid=28296 table_oid=1259 dump_id=5445
CREATE INDEX ix_idempotency_operations_lookup ON public.idempotency_operations USING btree (tenant_id, operation_id);

-- desc=INDEX tag='ix_notifications_unread' namespace='public' oid=28288 table_oid=1259 dump_id=5440
CREATE INDEX ix_notifications_unread ON public.notifications USING btree (tenant_id, user_id, created_at_utc DESC) WHERE (is_read = false);

-- desc=INDEX tag='ix_observations_criterion' namespace='public' oid=28280 table_oid=1259 dump_id=5396
CREATE INDEX ix_observations_criterion ON public.observations USING btree (tenant_id, audit_criterion_id, created_at_utc);

-- desc=INDEX tag='ix_outbox_pending' namespace='public' oid=28297 table_oid=1259 dump_id=5448
CREATE INDEX ix_outbox_pending ON public.outbox_messages USING btree (tenant_id, status, available_at_utc) WHERE ((status)::text = ANY ((ARRAY['PENDING'::character varying, 'FAILED'::character varying])::text[]));

-- desc=INDEX tag='ix_report_template_versions_template' namespace='public' oid=28294 table_oid=1259 dump_id=5428
CREATE INDEX ix_report_template_versions_template ON public.report_template_versions USING btree (tenant_id, report_template_id, version_number DESC);

-- desc=INDEX tag='ix_report_templates_active' namespace='public' oid=28293 table_oid=1259 dump_id=5423
CREATE INDEX ix_report_templates_active ON public.report_templates USING btree (tenant_id, report_type, is_active);

-- desc=INDEX tag='ix_users_tenant' namespace='public' oid=28265 table_oid=1259 dump_id=5290
CREATE INDEX ix_users_tenant ON public.users USING btree (tenant_id);

-- desc=INDEX tag='ix_users_tenant_active' namespace='public' oid=28266 table_oid=1259 dump_id=5291
CREATE INDEX ix_users_tenant_active ON public.users USING btree (tenant_id, is_active);

-- desc=INDEX tag='uq_audit_team_one_lead' namespace='public' oid=27532 table_oid=1259 dump_id=5389
CREATE UNIQUE INDEX uq_audit_team_one_lead ON public.audit_team USING btree (audit_id) WHERE ((audit_role)::text = 'LEAD'::text);

-- desc=INDEX tag='uq_audited_company_client_tax_id' namespace='public' oid=27144 table_oid=1259 dump_id=5349
CREATE UNIQUE INDEX uq_audited_company_client_tax_id ON public.audited_companies USING btree (tenant_id, client_id, tax_id) WHERE (tax_id IS NOT NULL);

-- desc=INDEX tag='uq_clients_tenant_tax_id' namespace='public' oid=27085 table_oid=1259 dump_id=5343
CREATE UNIQUE INDEX uq_clients_tenant_tax_id ON public.clients USING btree (tenant_id, tax_id) WHERE (tax_id IS NOT NULL);

-- ---- Funciones PL/pgSQL (6) — deben existir antes que los triggers ----
-- desc=FUNCTION tag='enforce_same_tenant_references()' namespace='public' oid=28334 table_oid=1255 dump_id=328
CREATE FUNCTION public.enforce_same_tenant_references() RETURNS trigger
    LANGUAGE plpgsql
    AS $_$
DECLARE
    i INTEGER;
    fk_value UUID;
    referenced_tenant UUID;
    column_name TEXT;
    referenced_table TEXT;
BEGIN
    IF TG_NARGS % 2 <> 0 THEN
        RAISE EXCEPTION 'enforce_same_tenant_references requiere pares columna/tabla';
    END IF;

    i := 0;
    WHILE i < TG_NARGS LOOP
        column_name := TG_ARGV[i];
        referenced_table := TG_ARGV[i + 1];

        fk_value := NULLIF(to_jsonb(NEW) ->> column_name, '')::UUID;

        IF fk_value IS NOT NULL THEN
            EXECUTE format(
                'SELECT tenant_id FROM %I WHERE id = $1',
                referenced_table
            )
            INTO referenced_tenant
            USING fk_value;

            IF referenced_tenant IS NULL THEN
                RAISE EXCEPTION
                    'Referencia inválida/no visible: %.% = %',
                    TG_TABLE_NAME, column_name, fk_value;
            END IF;

            IF referenced_tenant <> NEW.tenant_id THEN
                RAISE EXCEPTION
                    'Violación multitenant en %.%: tenant % no coincide con tenant referenciado %',
                    TG_TABLE_NAME, column_name, NEW.tenant_id, referenced_tenant;
            END IF;
        END IF;

        i := i + 2;
    END LOOP;

    RETURN NEW;
END;
$_$;

-- desc=FUNCTION tag='normalize_user_email()' namespace='public' oid=28367 table_oid=1255 dump_id=329
CREATE FUNCTION public.normalize_user_email() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.email := BTRIM(NEW.email);
    NEW.normalized_email := UPPER(BTRIM(NEW.email));
    RETURN NEW;
END;
$$;

-- desc=FUNCTION tag='prevent_audit_log_mutation()' namespace='public' oid=28371 table_oid=1255 dump_id=331
CREATE FUNCTION public.prevent_audit_log_mutation() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    RAISE EXCEPTION 'audit_logs es append-only: UPDATE/DELETE no están permitidos';
END;
$$;

-- desc=FUNCTION tag='prevent_final_report_mutation()' namespace='public' oid=28374 table_oid=1255 dump_id=332
CREATE FUNCTION public.prevent_final_report_mutation() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN

    -- Un reporte FINAL nunca se modifica ni elimina.
    IF OLD.status = 'FINAL' THEN
        RAISE EXCEPTION
            'Un reporte FINAL es inmutable; genere una nueva versión.';
    END IF;

    -- En DELETE PostgreSQL espera OLD.
    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    END IF;

    -- En UPDATE PostgreSQL espera NEW.
    RETURN NEW;

END;
$$;

-- desc=FUNCTION tag='set_updated_at_utc()' namespace='public' oid=26756 table_oid=1255 dump_id=316
CREATE FUNCTION public.set_updated_at_utc() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.updated_at_utc = NOW();
    RETURN NEW;
END;
$$;

-- desc=FUNCTION tag='validate_audit_before_close()' namespace='public' oid=28369 table_oid=1255 dump_id=330
CREATE FUNCTION public.validate_audit_before_close() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_status_code VARCHAR(40);
BEGIN
    SELECT code
      INTO new_status_code
      FROM audit_statuses
     WHERE id = NEW.status_id;

    IF new_status_code = 'CERRADA' THEN
        IF EXISTS (
            SELECT 1
              FROM audit_criteria ac
             WHERE ac.audit_id = NEW.id
               AND ac.tenant_id = NEW.tenant_id
               AND ac.is_mandatory_snapshot = TRUE
               AND ac.compliance_status_id IS NULL
        ) THEN
            RAISE EXCEPTION
                'No se puede cerrar la auditoría %: existen criterios obligatorios sin evaluar.',
                NEW.folio;
        END IF;

        IF NEW.validated_by_user_id IS NULL OR NEW.validated_at_utc IS NULL THEN
            RAISE EXCEPTION
                'No se puede cerrar la auditoría % sin validación del auditor líder.',
                NEW.folio;
        END IF;

        IF NEW.closed_at_utc IS NULL THEN
            NEW.closed_at_utc := NOW();
        END IF;
    END IF;

    RETURN NEW;
END;
$$;

-- ---- Triggers (54) ----
-- desc=TRIGGER tag='access_logs trg_access_logs_same_tenant' namespace='public' oid=28366 table_oid=2620 dump_id=5637
CREATE TRIGGER trg_access_logs_same_tenant BEFORE INSERT OR UPDATE ON public.access_logs FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('user_id', 'users');

-- desc=TRIGGER tag='audit_checklists trg_audit_checklists_same_tenant' namespace='public' oid=28349 table_oid=2620 dump_id=5609
CREATE TRIGGER trg_audit_checklists_same_tenant BEFORE INSERT OR UPDATE ON public.audit_checklists FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('audit_id', 'audits', 'checklist_version_id', 'checklist_versions');

-- desc=TRIGGER tag='audit_criteria trg_audit_criteria_same_tenant' namespace='public' oid=28351 table_oid=2620 dump_id=5611
CREATE TRIGGER trg_audit_criteria_same_tenant BEFORE INSERT OR UPDATE ON public.audit_criteria FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('audit_id', 'audits', 'audit_checklist_id', 'audit_checklists', 'criterion_id', 'criteria', 'evaluated_by_user_id', 'users');

-- desc=TRIGGER tag='audit_criteria trg_audit_criteria_updated_at' namespace='public' oid=28257 table_oid=2620 dump_id=5612
CREATE TRIGGER trg_audit_criteria_updated_at BEFORE UPDATE ON public.audit_criteria FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='audit_document_requests trg_document_requests_same_tenant' namespace='public' oid=28353 table_oid=2620 dump_id=5614
CREATE TRIGGER trg_document_requests_same_tenant BEFORE INSERT OR UPDATE ON public.audit_document_requests FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('audit_id', 'audits', 'requested_by_user_id', 'users');

-- desc=TRIGGER tag='audit_document_requests trg_document_requests_updated_at' namespace='public' oid=28262 table_oid=2620 dump_id=5615
CREATE TRIGGER trg_document_requests_updated_at BEFORE UPDATE ON public.audit_document_requests FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='audit_evidences trg_evidences_same_tenant' namespace='public' oid=28354 table_oid=2620 dump_id=5616
CREATE TRIGGER trg_evidences_same_tenant BEFORE INSERT OR UPDATE ON public.audit_evidences FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('audit_id', 'audits', 'audit_criterion_id', 'audit_criteria', 'finding_id', 'findings', 'corrective_action_id', 'corrective_actions', 'document_request_id', 'audit_document_requests', 'uploaded_by_user_id', 'users');

-- desc=TRIGGER tag='audit_logs trg_audit_logs_no_delete' namespace='public' oid=28373 table_oid=2620 dump_id=5634
CREATE TRIGGER trg_audit_logs_no_delete BEFORE DELETE ON public.audit_logs FOR EACH ROW EXECUTE FUNCTION public.prevent_audit_log_mutation();

-- desc=TRIGGER tag='audit_logs trg_audit_logs_no_update' namespace='public' oid=28372 table_oid=2620 dump_id=5635
CREATE TRIGGER trg_audit_logs_no_update BEFORE UPDATE ON public.audit_logs FOR EACH ROW EXECUTE FUNCTION public.prevent_audit_log_mutation();

-- desc=TRIGGER tag='audit_logs trg_audit_logs_same_tenant' namespace='public' oid=28365 table_oid=2620 dump_id=5636
CREATE TRIGGER trg_audit_logs_same_tenant BEFORE INSERT OR UPDATE ON public.audit_logs FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('user_id', 'users', 'audit_id', 'audits');

-- desc=TRIGGER tag='audit_programs trg_audit_programs_same_tenant' namespace='public' oid=28348 table_oid=2620 dump_id=5608
CREATE TRIGGER trg_audit_programs_same_tenant BEFORE INSERT OR UPDATE ON public.audit_programs FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('audit_id', 'audits');

-- desc=TRIGGER tag='audit_reports trg_audit_reports_final_immutable' namespace='public' oid=28375 table_oid=2620 dump_id=5628
CREATE TRIGGER trg_audit_reports_final_immutable BEFORE DELETE OR UPDATE ON public.audit_reports FOR EACH ROW EXECUTE FUNCTION public.prevent_final_report_mutation();

-- desc=TRIGGER tag='audit_reports trg_audit_reports_same_tenant' namespace='public' oid=28358 table_oid=2620 dump_id=5629
CREATE TRIGGER trg_audit_reports_same_tenant BEFORE INSERT OR UPDATE ON public.audit_reports FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('audit_id', 'audits', 'report_template_version_id', 'report_template_versions', 'generated_by_user_id', 'users', 'validated_by_user_id', 'users');

-- desc=TRIGGER tag='audit_results trg_audit_results_same_tenant' namespace='public' oid=28359 table_oid=2620 dump_id=5622
CREATE TRIGGER trg_audit_results_same_tenant BEFORE INSERT OR UPDATE ON public.audit_results FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('audit_id', 'audits', 'finalized_by_user_id', 'users');

-- desc=TRIGGER tag='audit_results trg_audit_results_updated_at' namespace='public' oid=28258 table_oid=2620 dump_id=5623
CREATE TRIGGER trg_audit_results_updated_at BEFORE UPDATE ON public.audit_results FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='audit_signatories trg_audit_signatories_same_tenant' namespace='public' oid=28362 table_oid=2620 dump_id=5630
CREATE TRIGGER trg_audit_signatories_same_tenant BEFORE INSERT OR UPDATE ON public.audit_signatories FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('audit_id', 'audits', 'user_id', 'users', 'client_contact_id', 'client_contacts');

-- desc=TRIGGER tag='audit_signatories trg_audit_signatories_updated_at' namespace='public' oid=28261 table_oid=2620 dump_id=5631
CREATE TRIGGER trg_audit_signatories_updated_at BEFORE UPDATE ON public.audit_signatories FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='audit_team trg_audit_team_same_tenant' namespace='public' oid=28350 table_oid=2620 dump_id=5610
CREATE TRIGGER trg_audit_team_same_tenant BEFORE INSERT OR UPDATE ON public.audit_team FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('audit_id', 'audits', 'user_id', 'users', 'assigned_by_user_id', 'users');

-- desc=TRIGGER tag='audited_companies trg_audited_companies_same_tenant' namespace='public' oid=28339 table_oid=2620 dump_id=5592
CREATE TRIGGER trg_audited_companies_same_tenant BEFORE INSERT OR UPDATE ON public.audited_companies FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('client_id', 'clients');

-- desc=TRIGGER tag='audited_companies trg_audited_companies_updated_at' namespace='public' oid=28251 table_oid=2620 dump_id=5593
CREATE TRIGGER trg_audited_companies_updated_at BEFORE UPDATE ON public.audited_companies FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='audits trg_audits_same_tenant' namespace='public' oid=28347 table_oid=2620 dump_id=5605
CREATE TRIGGER trg_audits_same_tenant BEFORE INSERT OR UPDATE ON public.audits FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('client_id', 'clients', 'audited_company_id', 'audited_companies', 'company_site_id', 'company_sites', 'created_by_user_id', 'users', 'validated_by_user_id', 'users');

-- desc=TRIGGER tag='audits trg_audits_updated_at' namespace='public' oid=28256 table_oid=2620 dump_id=5606
CREATE TRIGGER trg_audits_updated_at BEFORE UPDATE ON public.audits FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='audits trg_audits_validate_close' namespace='public' oid=28370 table_oid=2620 dump_id=5607
CREATE TRIGGER trg_audits_validate_close BEFORE INSERT OR UPDATE OF status_id ON public.audits FOR EACH ROW EXECUTE FUNCTION public.validate_audit_before_close();

-- desc=TRIGGER tag='checklist_sections trg_checklist_sections_same_tenant' namespace='public' oid=28345 table_oid=2620 dump_id=5603
CREATE TRIGGER trg_checklist_sections_same_tenant BEFORE INSERT OR UPDATE ON public.checklist_sections FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('checklist_version_id', 'checklist_versions');

-- desc=TRIGGER tag='checklist_versions trg_checklist_versions_same_tenant' namespace='public' oid=28344 table_oid=2620 dump_id=5601
CREATE TRIGGER trg_checklist_versions_same_tenant BEFORE INSERT OR UPDATE ON public.checklist_versions FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('checklist_id', 'checklists', 'created_by_user_id', 'users');

-- desc=TRIGGER tag='checklist_versions trg_checklist_versions_updated_at' namespace='public' oid=28255 table_oid=2620 dump_id=5602
CREATE TRIGGER trg_checklist_versions_updated_at BEFORE UPDATE ON public.checklist_versions FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='checklists trg_checklists_same_tenant' namespace='public' oid=28343 table_oid=2620 dump_id=5599
CREATE TRIGGER trg_checklists_same_tenant BEFORE INSERT OR UPDATE ON public.checklists FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('created_by_user_id', 'users');

-- desc=TRIGGER tag='checklists trg_checklists_updated_at' namespace='public' oid=28254 table_oid=2620 dump_id=5600
CREATE TRIGGER trg_checklists_updated_at BEFORE UPDATE ON public.checklists FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='client_contacts trg_client_contacts_same_tenant' namespace='public' oid=28341 table_oid=2620 dump_id=5596
CREATE TRIGGER trg_client_contacts_same_tenant BEFORE INSERT OR UPDATE ON public.client_contacts FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('client_id', 'clients', 'audited_company_id', 'audited_companies');

-- desc=TRIGGER tag='client_contacts trg_client_contacts_updated_at' namespace='public' oid=28253 table_oid=2620 dump_id=5597
CREATE TRIGGER trg_client_contacts_updated_at BEFORE UPDATE ON public.client_contacts FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='client_programs trg_client_programs_same_tenant' namespace='public' oid=28338 table_oid=2620 dump_id=5591
CREATE TRIGGER trg_client_programs_same_tenant BEFORE INSERT OR UPDATE ON public.client_programs FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('client_id', 'clients');

-- desc=TRIGGER tag='clients trg_clients_updated_at' namespace='public' oid=28250 table_oid=2620 dump_id=5590
CREATE TRIGGER trg_clients_updated_at BEFORE UPDATE ON public.clients FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='company_sites trg_company_sites_same_tenant' namespace='public' oid=28340 table_oid=2620 dump_id=5594
CREATE TRIGGER trg_company_sites_same_tenant BEFORE INSERT OR UPDATE ON public.company_sites FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('audited_company_id', 'audited_companies');

-- desc=TRIGGER tag='company_sites trg_company_sites_updated_at' namespace='public' oid=28252 table_oid=2620 dump_id=5595
CREATE TRIGGER trg_company_sites_updated_at BEFORE UPDATE ON public.company_sites FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='corrective_actions trg_corrective_actions_same_tenant' namespace='public' oid=28356 table_oid=2620 dump_id=5619
CREATE TRIGGER trg_corrective_actions_same_tenant BEFORE INSERT OR UPDATE ON public.corrective_actions FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('finding_id', 'findings', 'responsible_user_id', 'users', 'responsible_contact_id', 'client_contacts', 'validated_by_user_id', 'users', 'created_by_user_id', 'users');

-- desc=TRIGGER tag='corrective_actions trg_corrective_actions_updated_at' namespace='public' oid=28264 table_oid=2620 dump_id=5620
CREATE TRIGGER trg_corrective_actions_updated_at BEFORE UPDATE ON public.corrective_actions FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='criteria trg_criteria_same_tenant' namespace='public' oid=28346 table_oid=2620 dump_id=5604
CREATE TRIGGER trg_criteria_same_tenant BEFORE INSERT OR UPDATE ON public.criteria FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('checklist_section_id', 'checklist_sections');

-- desc=TRIGGER tag='finding_followups trg_finding_followups_same_tenant' namespace='public' oid=28357 table_oid=2620 dump_id=5621
CREATE TRIGGER trg_finding_followups_same_tenant BEFORE INSERT OR UPDATE ON public.finding_followups FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('finding_id', 'findings', 'corrective_action_id', 'corrective_actions', 'author_user_id', 'users');

-- desc=TRIGGER tag='findings trg_findings_same_tenant' namespace='public' oid=28355 table_oid=2620 dump_id=5617
CREATE TRIGGER trg_findings_same_tenant BEFORE INSERT OR UPDATE ON public.findings FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('audit_id', 'audits', 'audit_criterion_id', 'audit_criteria', 'responsible_user_id', 'users', 'responsible_contact_id', 'client_contacts', 'created_by_user_id', 'users', 'validated_by_user_id', 'users');

-- desc=TRIGGER tag='findings trg_findings_updated_at' namespace='public' oid=28263 table_oid=2620 dump_id=5618
CREATE TRIGGER trg_findings_updated_at BEFORE UPDATE ON public.findings FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='idempotency_operations trg_idempotency_same_tenant' namespace='public' oid=28363 table_oid=2620 dump_id=5633
CREATE TRIGGER trg_idempotency_same_tenant BEFORE INSERT OR UPDATE ON public.idempotency_operations FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('user_id', 'users');

-- desc=TRIGGER tag='notifications trg_notifications_same_tenant' namespace='public' oid=28364 table_oid=2620 dump_id=5632
CREATE TRIGGER trg_notifications_same_tenant BEFORE INSERT OR UPDATE ON public.notifications FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('user_id', 'users');

-- desc=TRIGGER tag='observations trg_observations_same_tenant' namespace='public' oid=28352 table_oid=2620 dump_id=5613
CREATE TRIGGER trg_observations_same_tenant BEFORE INSERT OR UPDATE ON public.observations FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('audit_id', 'audits', 'audit_criterion_id', 'audit_criteria', 'author_user_id', 'users');

-- desc=TRIGGER tag='password_reset_tokens trg_password_reset_same_tenant' namespace='public' oid=28336 table_oid=2620 dump_id=5588
CREATE TRIGGER trg_password_reset_same_tenant BEFORE INSERT OR UPDATE ON public.password_reset_tokens FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('user_id', 'users');

-- desc=TRIGGER tag='refresh_tokens trg_refresh_tokens_same_tenant' namespace='public' oid=28337 table_oid=2620 dump_id=5589
CREATE TRIGGER trg_refresh_tokens_same_tenant BEFORE INSERT OR UPDATE ON public.refresh_tokens FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('user_id', 'users');

-- desc=TRIGGER tag='report_template_versions trg_report_template_versions_same_tenant' namespace='public' oid=28361 table_oid=2620 dump_id=5626
CREATE TRIGGER trg_report_template_versions_same_tenant BEFORE INSERT OR UPDATE ON public.report_template_versions FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('report_template_id', 'report_templates', 'created_by_user_id', 'users');

-- desc=TRIGGER tag='report_template_versions trg_report_template_versions_updated_at' namespace='public' oid=28260 table_oid=2620 dump_id=5627
CREATE TRIGGER trg_report_template_versions_updated_at BEFORE UPDATE ON public.report_template_versions FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='report_templates trg_report_templates_same_tenant' namespace='public' oid=28360 table_oid=2620 dump_id=5624
CREATE TRIGGER trg_report_templates_same_tenant BEFORE INSERT OR UPDATE ON public.report_templates FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('created_by_user_id', 'users');

-- desc=TRIGGER tag='report_templates trg_report_templates_updated_at' namespace='public' oid=28259 table_oid=2620 dump_id=5625
CREATE TRIGGER trg_report_templates_updated_at BEFORE UPDATE ON public.report_templates FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='tenants trg_tenants_updated_at' namespace='public' oid=28248 table_oid=2620 dump_id=5584
CREATE TRIGGER trg_tenants_updated_at BEFORE UPDATE ON public.tenants FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();

-- desc=TRIGGER tag='user_client_access trg_user_client_access_same_tenant' namespace='public' oid=28342 table_oid=2620 dump_id=5598
CREATE TRIGGER trg_user_client_access_same_tenant BEFORE INSERT OR UPDATE ON public.user_client_access FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('user_id', 'users', 'client_id', 'clients', 'granted_by_user_id', 'users');

-- desc=TRIGGER tag='user_roles trg_user_roles_same_tenant' namespace='public' oid=28335 table_oid=2620 dump_id=5587
CREATE TRIGGER trg_user_roles_same_tenant BEFORE INSERT OR UPDATE ON public.user_roles FOR EACH ROW EXECUTE FUNCTION public.enforce_same_tenant_references('user_id', 'users', 'assigned_by_user_id', 'users');

-- desc=TRIGGER tag='users trg_users_normalize_email' namespace='public' oid=28368 table_oid=2620 dump_id=5585
CREATE TRIGGER trg_users_normalize_email BEFORE INSERT OR UPDATE OF email ON public.users FOR EACH ROW EXECUTE FUNCTION public.normalize_user_email();

-- desc=TRIGGER tag='users trg_users_updated_at' namespace='public' oid=28249 table_oid=2620 dump_id=5586
CREATE TRIGGER trg_users_updated_at BEFORE UPDATE ON public.users FOR EACH ROW EXECUTE FUNCTION public.set_updated_at_utc();
