-- =============================================================================
-- 002_security.sql — RLS, roles de prueba y ACL del baseline V2.1
-- =============================================================================
-- SOLO para BD desechable (Testcontainers/CI). Las contraseñas de abajo son
-- placeholders de prueba, nunca usados contra infraestructura real -- el
-- contenedor es efímero y no se expone en red. NO reutilizar estos valores
-- fuera de un entorno de test descartable.
--
-- Ejecutar DESPUÉS de 001_schema.sql, como el mismo superusuario por
-- defecto del contenedor (usualmente "postgres"). Este script:
--   1) crea procofa_owner / procofa_app si no existen,
--   2) reasigna el ownership de todas las tablas de "public" a
--      procofa_owner (para que FORCE ROW LEVEL SECURITY, ya aplicado en
--      001_schema.sql, tenga efecto real sobre el dueño),
--   3) habilita RLS (ENABLE ROW LEVEL SECURITY) y crea las 36 policies,
--   4) aplica el ACL real (GRANT/REVOKE) tal como existe en la BD real.
--
-- Los tests de integración deben conectarse como procofa_app (nunca como
-- el superusuario ni como procofa_owner) para que RLS/ACL se ejerzan de
-- verdad -- ver TenantIsolationTests en Procofa.IntegrationTests.
-- =============================================================================

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'procofa_owner') THEN
        CREATE ROLE procofa_owner WITH LOGIN PASSWORD 'test_only_owner_pw';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'procofa_app') THEN
        CREATE ROLE procofa_app WITH LOGIN PASSWORD 'test_only_app_pw' NOSUPERUSER NOCREATEDB NOCREATEROLE;
    END IF;
END
$$;

-- Reasignar ownership de todas las tablas de "public" a procofa_owner.
DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN SELECT tablename FROM pg_tables WHERE schemaname = 'public' LOOP
        EXECUTE format('ALTER TABLE public.%I OWNER TO procofa_owner', r.tablename);
    END LOOP;
END
$$;

-- ---- ENABLE ROW LEVEL SECURITY (36 tablas) ----
-- desc=ROW SECURITY tag='access_logs' namespace='public' oid=28224 table_oid=0 dump_id=5820
ALTER TABLE public.access_logs ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='audit_checklists' namespace='public' oid=27471 table_oid=0 dump_id=5802
ALTER TABLE public.audit_checklists ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='audit_criteria' namespace='public' oid=27533 table_oid=0 dump_id=5804
ALTER TABLE public.audit_criteria ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='audit_document_requests' namespace='public' oid=27627 table_oid=0 dump_id=5806
ALTER TABLE public.audit_document_requests ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='audit_evidences' namespace='public' oid=27662 table_oid=0 dump_id=5807
ALTER TABLE public.audit_evidences ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='audit_logs' namespace='public' oid=28195 table_oid=0 dump_id=5819
ALTER TABLE public.audit_logs ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='audit_programs' namespace='public' oid=27448 table_oid=0 dump_id=5801
ALTER TABLE public.audit_programs ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='audit_reports' namespace='public' oid=28014 table_oid=0 dump_id=5814
ALTER TABLE public.audit_reports ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='audit_results' namespace='public' oid=27894 table_oid=0 dump_id=5811
ALTER TABLE public.audit_results ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='audit_signatories' namespace='public' oid=28066 table_oid=0 dump_id=5815
ALTER TABLE public.audit_signatories ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='audit_team' namespace='public' oid=27500 table_oid=0 dump_id=5803
ALTER TABLE public.audit_team ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='audited_companies' namespace='public' oid=27109 table_oid=0 dump_id=5792
ALTER TABLE public.audited_companies ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='audits' namespace='public' oid=27377 table_oid=0 dump_id=5800
ALTER TABLE public.audits ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='checklist_sections' namespace='public' oid=27323 table_oid=0 dump_id=5798
ALTER TABLE public.checklist_sections ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='checklist_versions' namespace='public' oid=27285 table_oid=0 dump_id=5797
ALTER TABLE public.checklist_versions ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='checklists' namespace='public' oid=27240 table_oid=0 dump_id=5796
ALTER TABLE public.checklists ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='client_contacts' namespace='public' oid=27176 table_oid=0 dump_id=5794
ALTER TABLE public.client_contacts ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='client_programs' namespace='public' oid=27086 table_oid=0 dump_id=5791
ALTER TABLE public.client_programs ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='clients' namespace='public' oid=27063 table_oid=0 dump_id=5790
ALTER TABLE public.clients ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='company_sites' namespace='public' oid=27145 table_oid=0 dump_id=5793
ALTER TABLE public.company_sites ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='corrective_actions' namespace='public' oid=27791 table_oid=0 dump_id=5809
ALTER TABLE public.corrective_actions ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='criteria' namespace='public' oid=27347 table_oid=0 dump_id=5799
ALTER TABLE public.criteria ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='finding_followups' namespace='public' oid=27849 table_oid=0 dump_id=5810
ALTER TABLE public.finding_followups ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='findings' namespace='public' oid=27713 table_oid=0 dump_id=5808
ALTER TABLE public.findings ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='idempotency_operations' namespace='public' oid=28138 table_oid=0 dump_id=5817
ALTER TABLE public.idempotency_operations ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='notifications' namespace='public' oid=28107 table_oid=0 dump_id=5816
ALTER TABLE public.notifications ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='observations' namespace='public' oid=27588 table_oid=0 dump_id=5805
ALTER TABLE public.observations ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='outbox_messages' namespace='public' oid=28168 table_oid=0 dump_id=5818
ALTER TABLE public.outbox_messages ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='password_reset_tokens' namespace='public' oid=26880 table_oid=0 dump_id=5788
ALTER TABLE public.password_reset_tokens ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='refresh_tokens' namespace='public' oid=26905 table_oid=0 dump_id=5789
ALTER TABLE public.refresh_tokens ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='report_template_versions' namespace='public' oid=27975 table_oid=0 dump_id=5813
ALTER TABLE public.report_template_versions ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='report_templates' namespace='public' oid=27942 table_oid=0 dump_id=5812
ALTER TABLE public.report_templates ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='tenants' namespace='public' oid=26757 table_oid=0 dump_id=5785
ALTER TABLE public.tenants ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='user_client_access' namespace='public' oid=27210 table_oid=0 dump_id=5795
ALTER TABLE public.user_client_access ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='user_roles' namespace='public' oid=26850 table_oid=0 dump_id=5787
ALTER TABLE public.user_roles ENABLE ROW LEVEL SECURITY;

-- desc=ROW SECURITY tag='users' namespace='public' oid=26817 table_oid=0 dump_id=5786
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

-- ---- RLS Policies (36) ----
-- desc=POLICY tag='access_logs access_logs_tenant_isolation' namespace='public' oid=28333 table_oid=3256 dump_id=5856
CREATE POLICY access_logs_tenant_isolation ON public.access_logs USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='audit_checklists audit_checklists_tenant_isolation' namespace='public' oid=28315 table_oid=3256 dump_id=5838
CREATE POLICY audit_checklists_tenant_isolation ON public.audit_checklists USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='audit_criteria audit_criteria_tenant_isolation' namespace='public' oid=28317 table_oid=3256 dump_id=5840
CREATE POLICY audit_criteria_tenant_isolation ON public.audit_criteria USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='audit_document_requests audit_document_requests_tenant_isolation' namespace='public' oid=28319 table_oid=3256 dump_id=5842
CREATE POLICY audit_document_requests_tenant_isolation ON public.audit_document_requests USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='audit_evidences audit_evidences_tenant_isolation' namespace='public' oid=28320 table_oid=3256 dump_id=5843
CREATE POLICY audit_evidences_tenant_isolation ON public.audit_evidences USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='audit_logs audit_logs_tenant_isolation' namespace='public' oid=28332 table_oid=3256 dump_id=5855
CREATE POLICY audit_logs_tenant_isolation ON public.audit_logs USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='audit_programs audit_programs_tenant_isolation' namespace='public' oid=28314 table_oid=3256 dump_id=5837
CREATE POLICY audit_programs_tenant_isolation ON public.audit_programs USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='audit_reports audit_reports_tenant_isolation' namespace='public' oid=28327 table_oid=3256 dump_id=5850
CREATE POLICY audit_reports_tenant_isolation ON public.audit_reports USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='audit_results audit_results_tenant_isolation' namespace='public' oid=28324 table_oid=3256 dump_id=5847
CREATE POLICY audit_results_tenant_isolation ON public.audit_results USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='audit_signatories audit_signatories_tenant_isolation' namespace='public' oid=28328 table_oid=3256 dump_id=5851
CREATE POLICY audit_signatories_tenant_isolation ON public.audit_signatories USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='audit_team audit_team_tenant_isolation' namespace='public' oid=28316 table_oid=3256 dump_id=5839
CREATE POLICY audit_team_tenant_isolation ON public.audit_team USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='audited_companies audited_companies_tenant_isolation' namespace='public' oid=28305 table_oid=3256 dump_id=5828
CREATE POLICY audited_companies_tenant_isolation ON public.audited_companies USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='audits audits_tenant_isolation' namespace='public' oid=28313 table_oid=3256 dump_id=5836
CREATE POLICY audits_tenant_isolation ON public.audits USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='checklist_sections checklist_sections_tenant_isolation' namespace='public' oid=28311 table_oid=3256 dump_id=5834
CREATE POLICY checklist_sections_tenant_isolation ON public.checklist_sections USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='checklist_versions checklist_versions_tenant_isolation' namespace='public' oid=28310 table_oid=3256 dump_id=5833
CREATE POLICY checklist_versions_tenant_isolation ON public.checklist_versions USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='checklists checklists_tenant_isolation' namespace='public' oid=28309 table_oid=3256 dump_id=5832
CREATE POLICY checklists_tenant_isolation ON public.checklists USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='client_contacts client_contacts_tenant_isolation' namespace='public' oid=28307 table_oid=3256 dump_id=5830
CREATE POLICY client_contacts_tenant_isolation ON public.client_contacts USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='client_programs client_programs_tenant_isolation' namespace='public' oid=28304 table_oid=3256 dump_id=5827
CREATE POLICY client_programs_tenant_isolation ON public.client_programs USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='clients clients_tenant_isolation' namespace='public' oid=28303 table_oid=3256 dump_id=5826
CREATE POLICY clients_tenant_isolation ON public.clients USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='company_sites company_sites_tenant_isolation' namespace='public' oid=28306 table_oid=3256 dump_id=5829
CREATE POLICY company_sites_tenant_isolation ON public.company_sites USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='corrective_actions corrective_actions_tenant_isolation' namespace='public' oid=28322 table_oid=3256 dump_id=5845
CREATE POLICY corrective_actions_tenant_isolation ON public.corrective_actions USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='criteria criteria_tenant_isolation' namespace='public' oid=28312 table_oid=3256 dump_id=5835
CREATE POLICY criteria_tenant_isolation ON public.criteria USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='finding_followups finding_followups_tenant_isolation' namespace='public' oid=28323 table_oid=3256 dump_id=5846
CREATE POLICY finding_followups_tenant_isolation ON public.finding_followups USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='findings findings_tenant_isolation' namespace='public' oid=28321 table_oid=3256 dump_id=5844
CREATE POLICY findings_tenant_isolation ON public.findings USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='idempotency_operations idempotency_operations_tenant_isolation' namespace='public' oid=28329 table_oid=3256 dump_id=5852
CREATE POLICY idempotency_operations_tenant_isolation ON public.idempotency_operations USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='notifications notifications_tenant_isolation' namespace='public' oid=28331 table_oid=3256 dump_id=5854
CREATE POLICY notifications_tenant_isolation ON public.notifications USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='observations observations_tenant_isolation' namespace='public' oid=28318 table_oid=3256 dump_id=5841
CREATE POLICY observations_tenant_isolation ON public.observations USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='outbox_messages outbox_messages_tenant_isolation' namespace='public' oid=28330 table_oid=3256 dump_id=5853
CREATE POLICY outbox_messages_tenant_isolation ON public.outbox_messages USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='password_reset_tokens password_reset_tokens_tenant_isolation' namespace='public' oid=28301 table_oid=3256 dump_id=5824
CREATE POLICY password_reset_tokens_tenant_isolation ON public.password_reset_tokens USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='refresh_tokens refresh_tokens_tenant_isolation' namespace='public' oid=28302 table_oid=3256 dump_id=5825
CREATE POLICY refresh_tokens_tenant_isolation ON public.refresh_tokens USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='report_template_versions report_template_versions_tenant_isolation' namespace='public' oid=28326 table_oid=3256 dump_id=5849
CREATE POLICY report_template_versions_tenant_isolation ON public.report_template_versions USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='report_templates report_templates_tenant_isolation' namespace='public' oid=28325 table_oid=3256 dump_id=5848
CREATE POLICY report_templates_tenant_isolation ON public.report_templates USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='tenants tenants_isolation' namespace='public' oid=28298 table_oid=3256 dump_id=5821
CREATE POLICY tenants_isolation ON public.tenants USING ((id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='user_client_access user_client_access_tenant_isolation' namespace='public' oid=28308 table_oid=3256 dump_id=5831
CREATE POLICY user_client_access_tenant_isolation ON public.user_client_access USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='user_roles user_roles_tenant_isolation' namespace='public' oid=28300 table_oid=3256 dump_id=5823
CREATE POLICY user_roles_tenant_isolation ON public.user_roles USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- desc=POLICY tag='users users_tenant_isolation' namespace='public' oid=28299 table_oid=3256 dump_id=5822
CREATE POLICY users_tenant_isolation ON public.users USING ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid)) WITH CHECK ((tenant_id = (NULLIF(current_setting('app.tenant_id'::text, true), ''::text))::uuid));

-- ---- ACL: GRANT/REVOKE (50 sentencias, tal como existen en la BD real) ----
-- NOTA: las sentencias abajo referencian la base de datos 'procofa_audit_db'
-- por nombre -- válido igual en la BD desechable de Testcontainers si se
-- crea con ese mismo nombre (recomendado, ver README.md).
-- desc=ACL tag='DATABASE procofa_audit_db' namespace='' oid=0 table_oid=0 dump_id=5911
-- REVOKE CONNECT,TEMPORARY ON DATABASE procofa_audit_db FROM PUBLIC;
-- GRANT CONNECT ON DATABASE procofa_audit_db TO procofa_app;
-- Database-level CONNECT/TEMPORARY privileges are environment-specific.
-- They must be applied by deployment/provisioning using the actual database name.
-- They are intentionally excluded from this portable baseline.AccessLogConfiguration.cs

-- desc=ACL tag='SCHEMA public' namespace='' oid=0 table_oid=0 dump_id=5912
GRANT USAGE ON SCHEMA public TO procofa_app;

-- desc=ACL tag='TABLE access_logs' namespace='public' oid=0 table_oid=0 dump_id=5915
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.access_logs TO procofa_app;

-- desc=ACL tag='TABLE audit_checklists' namespace='public' oid=0 table_oid=0 dump_id=5916
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.audit_checklists TO procofa_app;

-- desc=ACL tag='TABLE audit_criteria' namespace='public' oid=0 table_oid=0 dump_id=5917
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.audit_criteria TO procofa_app;

-- desc=ACL tag='TABLE audit_document_requests' namespace='public' oid=0 table_oid=0 dump_id=5918
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.audit_document_requests TO procofa_app;

-- desc=ACL tag='TABLE audit_evidences' namespace='public' oid=0 table_oid=0 dump_id=5919
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.audit_evidences TO procofa_app;

-- desc=ACL tag='TABLE audit_logs' namespace='public' oid=0 table_oid=0 dump_id=5920
GRANT SELECT,INSERT ON TABLE public.audit_logs TO procofa_app;

-- desc=ACL tag='TABLE audit_programs' namespace='public' oid=0 table_oid=0 dump_id=5921
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.audit_programs TO procofa_app;

-- desc=ACL tag='TABLE audit_reports' namespace='public' oid=0 table_oid=0 dump_id=5922
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.audit_reports TO procofa_app;

-- desc=ACL tag='TABLE audit_results' namespace='public' oid=0 table_oid=0 dump_id=5923
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.audit_results TO procofa_app;

-- desc=ACL tag='TABLE audit_signatories' namespace='public' oid=0 table_oid=0 dump_id=5924
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.audit_signatories TO procofa_app;

-- desc=ACL tag='TABLE audit_statuses' namespace='public' oid=0 table_oid=0 dump_id=5925
GRANT SELECT ON TABLE public.audit_statuses TO procofa_app;

-- desc=ACL tag='TABLE audit_team' namespace='public' oid=0 table_oid=0 dump_id=5926
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.audit_team TO procofa_app;

-- desc=ACL tag='TABLE audit_types' namespace='public' oid=0 table_oid=0 dump_id=5927
GRANT SELECT ON TABLE public.audit_types TO procofa_app;

-- desc=ACL tag='TABLE audited_companies' namespace='public' oid=0 table_oid=0 dump_id=5928
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.audited_companies TO procofa_app;

-- desc=ACL tag='TABLE audits' namespace='public' oid=0 table_oid=0 dump_id=5929
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.audits TO procofa_app;

-- desc=ACL tag='TABLE checklist_sections' namespace='public' oid=0 table_oid=0 dump_id=5930
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.checklist_sections TO procofa_app;

-- desc=ACL tag='TABLE checklist_versions' namespace='public' oid=0 table_oid=0 dump_id=5931
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.checklist_versions TO procofa_app;

-- desc=ACL tag='TABLE checklists' namespace='public' oid=0 table_oid=0 dump_id=5932
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.checklists TO procofa_app;

-- desc=ACL tag='TABLE client_contacts' namespace='public' oid=0 table_oid=0 dump_id=5933
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.client_contacts TO procofa_app;

-- desc=ACL tag='TABLE client_programs' namespace='public' oid=0 table_oid=0 dump_id=5934
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.client_programs TO procofa_app;

-- desc=ACL tag='TABLE clients' namespace='public' oid=0 table_oid=0 dump_id=5935
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.clients TO procofa_app;

-- desc=ACL tag='TABLE company_sites' namespace='public' oid=0 table_oid=0 dump_id=5936
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.company_sites TO procofa_app;

-- desc=ACL tag='TABLE compliance_statuses' namespace='public' oid=0 table_oid=0 dump_id=5937
GRANT SELECT ON TABLE public.compliance_statuses TO procofa_app;

-- desc=ACL tag='TABLE corrective_action_statuses' namespace='public' oid=0 table_oid=0 dump_id=5938
GRANT SELECT ON TABLE public.corrective_action_statuses TO procofa_app;

-- desc=ACL tag='TABLE corrective_actions' namespace='public' oid=0 table_oid=0 dump_id=5939
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.corrective_actions TO procofa_app;

-- desc=ACL tag='TABLE criteria' namespace='public' oid=0 table_oid=0 dump_id=5940
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.criteria TO procofa_app;

-- desc=ACL tag='TABLE finding_followups' namespace='public' oid=0 table_oid=0 dump_id=5941
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.finding_followups TO procofa_app;

-- desc=ACL tag='TABLE finding_priorities' namespace='public' oid=0 table_oid=0 dump_id=5942
GRANT SELECT ON TABLE public.finding_priorities TO procofa_app;

-- desc=ACL tag='TABLE finding_statuses' namespace='public' oid=0 table_oid=0 dump_id=5943
GRANT SELECT ON TABLE public.finding_statuses TO procofa_app;

-- desc=ACL tag='TABLE finding_types' namespace='public' oid=0 table_oid=0 dump_id=5944
GRANT SELECT ON TABLE public.finding_types TO procofa_app;

-- desc=ACL tag='TABLE findings' namespace='public' oid=0 table_oid=0 dump_id=5945
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.findings TO procofa_app;

-- desc=ACL tag='TABLE idempotency_operations' namespace='public' oid=0 table_oid=0 dump_id=5946
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.idempotency_operations TO procofa_app;

-- desc=ACL tag='TABLE notifications' namespace='public' oid=0 table_oid=0 dump_id=5947
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.notifications TO procofa_app;

-- desc=ACL tag='TABLE observations' namespace='public' oid=0 table_oid=0 dump_id=5948
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.observations TO procofa_app;

-- desc=ACL tag='TABLE outbox_messages' namespace='public' oid=0 table_oid=0 dump_id=5949
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.outbox_messages TO procofa_app;

-- desc=ACL tag='TABLE password_reset_tokens' namespace='public' oid=0 table_oid=0 dump_id=5950
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.password_reset_tokens TO procofa_app;

-- desc=ACL tag='TABLE permissions' namespace='public' oid=0 table_oid=0 dump_id=5951
GRANT SELECT ON TABLE public.permissions TO procofa_app;

-- desc=ACL tag='TABLE profiles' namespace='public' oid=0 table_oid=0 dump_id=5952
GRANT SELECT ON TABLE public.profiles TO procofa_app;

-- desc=ACL tag='TABLE programs' namespace='public' oid=0 table_oid=0 dump_id=5953
GRANT SELECT ON TABLE public.programs TO procofa_app;

-- desc=ACL tag='TABLE refresh_tokens' namespace='public' oid=0 table_oid=0 dump_id=5954
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.refresh_tokens TO procofa_app;

-- desc=ACL tag='TABLE report_template_versions' namespace='public' oid=0 table_oid=0 dump_id=5955
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.report_template_versions TO procofa_app;

-- desc=ACL tag='TABLE report_templates' namespace='public' oid=0 table_oid=0 dump_id=5956
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.report_templates TO procofa_app;

-- desc=ACL tag='TABLE role_permissions' namespace='public' oid=0 table_oid=0 dump_id=5957
GRANT SELECT ON TABLE public.role_permissions TO procofa_app;

-- desc=ACL tag='TABLE roles' namespace='public' oid=0 table_oid=0 dump_id=5958
GRANT SELECT ON TABLE public.roles TO procofa_app;

-- desc=ACL tag='TABLE tenants' namespace='public' oid=0 table_oid=0 dump_id=5959
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.tenants TO procofa_app;

-- desc=ACL tag='TABLE user_client_access' namespace='public' oid=0 table_oid=0 dump_id=5960
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.user_client_access TO procofa_app;

-- desc=ACL tag='TABLE user_roles' namespace='public' oid=0 table_oid=0 dump_id=5961
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.user_roles TO procofa_app;

-- desc=ACL tag='TABLE users' namespace='public' oid=0 table_oid=0 dump_id=5962
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.users TO procofa_app;
