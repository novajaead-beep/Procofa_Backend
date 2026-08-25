-- =============================================================================
-- 003_seed_catalogs.sql — Datos de catálogo (13 tablas) del baseline V2.1
-- =============================================================================
-- Generado a partir de procofa_bdFinal.sql (dump PGDMP real de
-- procofa_audit_db) — SOLO catálogos/seed de referencia (roles, permisos,
-- catálogos de estado/tipo, y el tenant fijo de Etapa 1). NO contiene datos
-- transaccionales, PII de clientes/usuarios, contraseñas ni secretos: todas
-- las tablas seedeadas aquí son catálogos de solo-lectura o de bootstrap de
-- seguridad (roles/permisos/tenant), no datos de negocio reales.
--
-- Orden de carga: respeta las FKs (role_permissions depende de roles y
-- permissions; el resto son catálogos independientes o el tenant raíz).
-- Idempotente: usa ON CONFLICT (id) DO NOTHING para poder re-ejecutarse
-- contra una BD ya seedeada sin fallar (p. ej. reintentos en Testcontainers).
-- =============================================================================

-- ---- tenants (1 filas) ----
INSERT INTO public.tenants (id, name, slug, legal_name, tax_id, is_active, created_at_utc, updated_at_utc)
VALUES
    ('00000000-0000-0000-0000-000000000001', 'PROCOFA', 'procofa', 'PROCOFA', NULL, true, '2026-08-23 22:37:14.369588+00', '2026-08-23 22:37:14.369588+00')
ON CONFLICT (id) DO NOTHING;

-- ---- roles (5 filas) ----
INSERT INTO public.roles (id, code, name, description)
VALUES
    ('b8e014e2-5685-4c37-88ee-87a7ad57d638', 'ADMIN', 'Administrador', 'Administra usuarios, clientes y catálogos. Consulta auditorías y bitácoras.'),
    ('4eb2f311-4c51-406f-89c0-cc7c46eac7bb', 'AUDITOR_LIDER', 'Auditor Líder', 'Crea, asigna y ejecuta auditorías; registra y valida hallazgos, genera reportes y valida cierres.'),
    ('7d19cc5a-dd99-4e21-ae87-00efd84835f1', 'AUDITOR_APOYO', 'Auditor de Apoyo', 'Participa en auditorías asignadas, captura respuestas y evidencias y propone hallazgos.'),
    ('ab7e6ba3-1ae5-4aac-b47c-1f5b50593e45', 'CLIENTE', 'Cliente / Empresa Auditada', 'Consulta sus auditorías, progreso y hallazgos; responde acciones correctivas y carga evidencias de cierre.'),
    ('2f4b1d47-950c-4422-b5cf-78df8793a8cf', 'CONSULTOR', 'Auditor / Usuario de Consulta', 'Solo lectura de auditorías, avance y reportes autorizados.')
ON CONFLICT (id) DO NOTHING;

-- ---- permissions (17 filas) ----
INSERT INTO public.permissions (id, code, name, description)
VALUES
    ('ea0df01c-169c-4fa4-8b81-ef65db6f8e23', 'USERS_MANAGE', 'Administrar usuarios', NULL),
    ('9fe757b0-575a-44bb-83a7-9fb0ac0c79b5', 'CLIENTS_MANAGE', 'Administrar clientes', NULL),
    ('12c92f82-3172-4090-b4f1-ffc20ed5d314', 'CATALOGS_MANAGE', 'Administrar catálogos y checklists', NULL),
    ('1d678d72-ee79-4d59-b12b-d636742787f9', 'AUDITS_READ', 'Consultar auditorías', NULL),
    ('91aac3b3-400d-4b87-ab89-9f301712eb5a', 'AUDITS_CREATE', 'Crear auditorías', NULL),
    ('594cdf95-8217-4837-a9cb-52ea16a39a53', 'AUDITS_EDIT_ASSIGNED', 'Editar auditorías asignadas', NULL),
    ('f821f84b-bf30-4412-a286-f197cb70b998', 'AUDITS_ASSIGN_TEAM', 'Asignar equipo auditor', NULL),
    ('027e7268-e02d-4809-997e-d21cad5fb1a5', 'CRITERIA_EVALUATE', 'Evaluar criterios', NULL),
    ('87914736-c30f-4598-903b-195091eaa131', 'EVIDENCE_UPLOAD', 'Cargar evidencias', NULL),
    ('0fcad70a-80c5-4a1e-92ea-102b0c266775', 'FINDINGS_CREATE', 'Crear/proponer hallazgos', NULL),
    ('435350fc-c770-4564-87d3-9a7820e87206', 'FINDINGS_VALIDATE', 'Validar hallazgos', NULL),
    ('570b46c3-bf71-4196-8d90-4dfaa928ae9e', 'CORRECTIVE_ACTION_RESPOND', 'Responder acciones correctivas', NULL),
    ('9afd2673-4684-42e3-96e6-817592677c3c', 'CORRECTIVE_ACTION_VALIDATE', 'Validar acciones correctivas', NULL),
    ('2166cbf8-e07c-4149-bcbd-cec764c994ee', 'REPORTS_READ', 'Consultar reportes', NULL),
    ('828ab506-fe57-43e2-aa34-fb0812fb02ef', 'REPORTS_GENERATE', 'Generar reportes', NULL),
    ('edbabd98-51d0-4568-abc7-14ff997b712b', 'REPORTS_VALIDATE', 'Validar reportes', NULL),
    ('1edeeef6-2dc0-4863-856c-24a6df1471f9', 'AUDIT_LOG_READ', 'Consultar bitácora', NULL)
ON CONFLICT (id) DO NOTHING;

-- ---- role_permissions (30 filas) ----
INSERT INTO public.role_permissions (role_id, permission_id)
VALUES
    ('b8e014e2-5685-4c37-88ee-87a7ad57d638', 'ea0df01c-169c-4fa4-8b81-ef65db6f8e23'),
    ('b8e014e2-5685-4c37-88ee-87a7ad57d638', '9fe757b0-575a-44bb-83a7-9fb0ac0c79b5'),
    ('b8e014e2-5685-4c37-88ee-87a7ad57d638', '12c92f82-3172-4090-b4f1-ffc20ed5d314'),
    ('b8e014e2-5685-4c37-88ee-87a7ad57d638', '1d678d72-ee79-4d59-b12b-d636742787f9'),
    ('b8e014e2-5685-4c37-88ee-87a7ad57d638', '2166cbf8-e07c-4149-bcbd-cec764c994ee'),
    ('b8e014e2-5685-4c37-88ee-87a7ad57d638', '1edeeef6-2dc0-4863-856c-24a6df1471f9'),
    ('4eb2f311-4c51-406f-89c0-cc7c46eac7bb', '1d678d72-ee79-4d59-b12b-d636742787f9'),
    ('4eb2f311-4c51-406f-89c0-cc7c46eac7bb', '91aac3b3-400d-4b87-ab89-9f301712eb5a'),
    ('4eb2f311-4c51-406f-89c0-cc7c46eac7bb', '594cdf95-8217-4837-a9cb-52ea16a39a53'),
    ('4eb2f311-4c51-406f-89c0-cc7c46eac7bb', 'f821f84b-bf30-4412-a286-f197cb70b998'),
    ('4eb2f311-4c51-406f-89c0-cc7c46eac7bb', '027e7268-e02d-4809-997e-d21cad5fb1a5'),
    ('4eb2f311-4c51-406f-89c0-cc7c46eac7bb', '87914736-c30f-4598-903b-195091eaa131'),
    ('4eb2f311-4c51-406f-89c0-cc7c46eac7bb', '0fcad70a-80c5-4a1e-92ea-102b0c266775'),
    ('4eb2f311-4c51-406f-89c0-cc7c46eac7bb', '435350fc-c770-4564-87d3-9a7820e87206'),
    ('4eb2f311-4c51-406f-89c0-cc7c46eac7bb', '9afd2673-4684-42e3-96e6-817592677c3c'),
    ('4eb2f311-4c51-406f-89c0-cc7c46eac7bb', '2166cbf8-e07c-4149-bcbd-cec764c994ee'),
    ('4eb2f311-4c51-406f-89c0-cc7c46eac7bb', '828ab506-fe57-43e2-aa34-fb0812fb02ef'),
    ('4eb2f311-4c51-406f-89c0-cc7c46eac7bb', 'edbabd98-51d0-4568-abc7-14ff997b712b'),
    ('7d19cc5a-dd99-4e21-ae87-00efd84835f1', '1d678d72-ee79-4d59-b12b-d636742787f9'),
    ('7d19cc5a-dd99-4e21-ae87-00efd84835f1', '594cdf95-8217-4837-a9cb-52ea16a39a53'),
    ('7d19cc5a-dd99-4e21-ae87-00efd84835f1', '027e7268-e02d-4809-997e-d21cad5fb1a5'),
    ('7d19cc5a-dd99-4e21-ae87-00efd84835f1', '87914736-c30f-4598-903b-195091eaa131'),
    ('7d19cc5a-dd99-4e21-ae87-00efd84835f1', '0fcad70a-80c5-4a1e-92ea-102b0c266775'),
    ('7d19cc5a-dd99-4e21-ae87-00efd84835f1', '2166cbf8-e07c-4149-bcbd-cec764c994ee'),
    ('ab7e6ba3-1ae5-4aac-b47c-1f5b50593e45', '1d678d72-ee79-4d59-b12b-d636742787f9'),
    ('ab7e6ba3-1ae5-4aac-b47c-1f5b50593e45', '87914736-c30f-4598-903b-195091eaa131'),
    ('ab7e6ba3-1ae5-4aac-b47c-1f5b50593e45', '570b46c3-bf71-4196-8d90-4dfaa928ae9e'),
    ('ab7e6ba3-1ae5-4aac-b47c-1f5b50593e45', '2166cbf8-e07c-4149-bcbd-cec764c994ee'),
    ('2f4b1d47-950c-4422-b5cf-78df8793a8cf', '1d678d72-ee79-4d59-b12b-d636742787f9'),
    ('2f4b1d47-950c-4422-b5cf-78df8793a8cf', '2166cbf8-e07c-4149-bcbd-cec764c994ee')
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- ---- profiles (6 filas) ----
INSERT INTO public.profiles (id, code, name, description, is_active)
VALUES
    ('5cc4f4fc-4f19-4778-a6b5-49f22633f169', 'MAQUILA', 'Maquiladora', NULL, true),
    ('7aa25b31-9ae7-4539-9508-68747d667131', 'TRANSPORTISTA', 'Transportista', NULL, true),
    ('9025990a-d159-49df-8c7f-7218b3f02783', 'AGENTE_ADUANAL', 'Agente aduanal', NULL, true),
    ('23ecef60-ffc3-4847-9f54-20388091a296', '3PL', 'Operador logístico / 3PL', NULL, true),
    ('4a5c3a64-b5fb-4005-b10e-86a3ac7c5661', 'SOCIO_COMERCIAL', 'Socio comercial', NULL, true),
    ('8e3e73c7-ee53-4fef-9097-b66aa2a0864c', 'OTRO', 'Otro', NULL, true)
ON CONFLICT (id) DO NOTHING;

-- ---- programs (2 filas) ----
INSERT INTO public.programs (id, code, name, description, is_active)
VALUES
    ('b7355bb4-4c26-4e2c-adcf-adc92bed2e27', 'OEA', 'Operador Económico Autorizado', 'Programa OEA.', true),
    ('5c0b4703-cdc8-437f-a7d5-84b4a08e6c90', 'CTPAT', 'C-TPAT', 'Customs-Trade Partnership Against Terrorism.', true)
ON CONFLICT (id) DO NOTHING;

-- ---- audit_statuses (7 filas) ----
INSERT INTO public.audit_statuses (id, code, name, sort_order, is_terminal)
VALUES
    ('68297744-3398-46aa-bd97-0bb5e7ebda6b', 'BORRADOR', 'Borrador', 10, false),
    ('283f2caa-8883-4650-8603-cec681123c44', 'PROGRAMADA', 'Programada', 20, false),
    ('9de6a721-3697-416d-9b72-48dd5bfa9f7f', 'EN_PROCESO', 'En proceso', 30, false),
    ('25f8e8c3-b098-4474-8a13-f1aa6401abef', 'REVISION', 'En revisión', 40, false),
    ('4236dffa-7a32-4688-92f0-5978df25c438', 'SEGUIMIENTO', 'En seguimiento', 50, false),
    ('437f00c3-6f08-4b3f-87f5-9748aedec196', 'CERRADA', 'Cerrada', 60, true),
    ('f835c2e9-7c5f-4e8e-a2b6-99806bc8e370', 'CANCELADA', 'Cancelada', 70, true)
ON CONFLICT (id) DO NOTHING;

-- ---- audit_types (6 filas) ----
INSERT INTO public.audit_types (id, code, name, description, is_active)
VALUES
    ('d1f1ce6e-b80d-42dc-867d-111a79ae35da', 'INTERNA_OEA', 'Auditoría interna OEA', NULL, true),
    ('1e86e062-4605-4853-9f18-e9fa99d993e5', 'INTERNA_CTPAT', 'Auditoría interna C-TPAT', NULL, true),
    ('dd65ae91-ee57-4723-a07d-ac783a38cb93', 'SOCIO_COMERCIAL', 'Auditoría a socio comercial', NULL, true),
    ('80721a81-6000-499e-b53c-6c8b62e848ff', 'DOCUMENTAL', 'Auditoría documental', NULL, true),
    ('23a75f73-4760-4d46-a1b7-e342ac3fa78e', 'EN_SITIO', 'Auditoría en sitio', NULL, true),
    ('f8540ed9-2802-48fc-a772-8a296d4029da', 'SEGUIMIENTO', 'Auditoría de seguimiento', NULL, true)
ON CONFLICT (id) DO NOTHING;

-- ---- compliance_statuses (4 filas) ----
INSERT INTO public.compliance_statuses (id, code, name, score_weight, included_in_score, sort_order)
VALUES
    ('57823b17-17b1-4566-8b31-910e00021d4d', 'CUMPLE', 'Cumple', 100.00, true, 10),
    ('6265a07a-27e6-48a7-9397-d50b7ef48125', 'CUMPLE_PARCIAL', 'Cumple parcialmente', 50.00, true, 20),
    ('76d17a39-e710-4bc2-b948-6dffac98d2b4', 'NO_CUMPLE', 'No cumple', 0.00, true, 30),
    ('e7760192-500d-4d13-8ea8-f2f22402b01f', 'NO_APLICA', 'No aplica', NULL, false, 40)
ON CONFLICT (id) DO NOTHING;

-- ---- finding_types (3 filas) ----
INSERT INTO public.finding_types (id, code, name, description)
VALUES
    ('d873682c-8c9a-4da4-8d21-32481c2a58b3', 'NO_CONFORMIDAD', 'No conformidad', 'Incumplimiento de requisito aplicable.'),
    ('511fdb72-500c-40f0-8fb3-03f492cecc59', 'OBSERVACION', 'Observación', 'Situación relevante que requiere atención.'),
    ('583d6ada-9462-4889-88cb-d5571b8e9fec', 'OPORTUNIDAD_MEJORA', 'Oportunidad de mejora', 'Recomendación para fortalecer el cumplimiento.')
ON CONFLICT (id) DO NOTHING;

-- ---- finding_priorities (3 filas) ----
INSERT INTO public.finding_priorities (id, code, name, sort_order)
VALUES
    ('c2ef6f7a-3663-4948-b164-51b0727dce07', 'ALTA', 'Alta', 10),
    ('3005835e-5c20-490f-a89a-1ccc96f014c7', 'MEDIA', 'Media', 20),
    ('084a0102-03c1-42ad-9ade-57cc3e7654f7', 'BAJA', 'Baja', 30)
ON CONFLICT (id) DO NOTHING;

-- ---- finding_statuses (5 filas) ----
INSERT INTO public.finding_statuses (id, code, name, is_closed, sort_order)
VALUES
    ('d8238994-b6fc-4c97-9416-c5e6b4e98c04', 'ABIERTO', 'Abierto', false, 10),
    ('a69cf6ad-36ed-48d1-a0c1-ac9ee59228cd', 'EN_PROCESO', 'En proceso', false, 20),
    ('e98f5fd6-21c1-4d20-8696-adf2c3380bbe', 'PENDIENTE_VALIDACION', 'Pendiente de validación', false, 30),
    ('ca8fe625-3ba6-4030-a609-a897192386e4', 'CERRADO', 'Cerrado', true, 40),
    ('c70d9309-2a6b-44eb-bc12-268aac1d1468', 'RECHAZADO', 'Rechazado', false, 50)
ON CONFLICT (id) DO NOTHING;

-- ---- corrective_action_statuses (6 filas) ----
INSERT INTO public.corrective_action_statuses (id, code, name, is_closed, sort_order)
VALUES
    ('339f1085-3fd5-478a-81a6-5d4dcdc9e80a', 'PENDIENTE', 'Pendiente', false, 10),
    ('a834d33e-ecdc-43f5-8b1a-19cc022d2553', 'EN_PROCESO', 'En proceso', false, 20),
    ('bba7a66c-aa91-4114-a729-791cb8edff04', 'PENDIENTE_VALIDACION', 'Pendiente de validación', false, 30),
    ('a1c6864f-f132-48c2-b5ff-00403b3ff5e9', 'VALIDADA', 'Validada', true, 40),
    ('e2a555e3-d938-436c-9caf-3ceba2bcd43b', 'RECHAZADA', 'Rechazada', false, 50),
    ('aa1e8a2b-60b5-4bff-a900-7db48e3a9e9b', 'CANCELADA', 'Cancelada', true, 60)
ON CONFLICT (id) DO NOTHING;

