using Microsoft.EntityFrameworkCore;
using Npgsql;
using Procofa.Infrastructure.Persistence;
using Procofa.Infrastructure.Persistence.Interceptors;
using Testcontainers.PostgreSql;

namespace Procofa.IntegrationTests.Fixtures;

/// <summary>
/// Fixture compartida (xUnit <see cref="ICollectionFixture{TFixture}"/>) —
/// UN solo contenedor PostgreSQL 18 desechable para TODA la suite de
/// integration tests (Instrucción 03: "PostgreSQL 18 desechable vía
/// Testcontainers — decisión definitiva, nunca PostgreSQL 16"). Levanta el
/// contenedor una vez y carga
/// <c>db/baseline/v2.1/{001_schema.sql,002_security.sql,003_seed_catalogs.sql}</c>
/// en ese orden — el MISMO bootstrap reproducible que usaría cualquier
/// desarrollador local (ver <c>db/baseline/v2.1/README.md</c>).
///
/// Expone dos connection strings: <see cref="SuperuserConnectionString"/>
/// (SOLO para bootstrap/semillas de datos de prueba entre tests — nunca
/// para las aserciones de RLS/ACL en sí) y <see cref="AppConnectionString"/>
/// (autenticada como <c>procofa_app</c> — la que deben usar TODAS las
/// queries que ejercen RLS/ACL de verdad: Instrucción 03, sección 27,
/// "las pruebas de RLS corren como procofa_app, nunca como superusuario").
///
/// Aislamiento entre tests: cada test crea su PROPIO tenant (GUID nuevo vía
/// <see cref="CreateTenantAsync"/>) en lugar de depender de un
/// TRUNCATE/reset global — como todas las tablas relevantes son
/// tenant-scoped y RLS es fail-closed, un tenant nuevo por test garantiza
/// aislamiento sin importar el orden de ejecución de xUnit ni requerir
/// enumerar las 48 tablas en orden de dependencia para un TRUNCATE seguro.
///
/// NO ejecutada por Claude en este sandbox: Docker/Docker Hub no son
/// alcanzables (egress allowlist) ni en el sandbox cloud ni en la VM del
/// bridge del usuario — ver sección J/L del reporte de Instrucción 03.
/// Escrita para compilar y correr tal cual el día que alguien la ejecute
/// con Docker disponible.
/// </summary>
public sealed class PostgresBaselineFixture : IAsyncLifetime
{
    private const string TestDatabaseName = "procofa_test";
    private const string AppRolePassword = "test_only_app_pw";

    private readonly PostgreSqlContainer _container =
    new PostgreSqlBuilder("postgres:18")
        .WithDatabase(TestDatabaseName)
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    /// <summary>SOLO para bootstrap/semillas de datos de prueba — nunca para las aserciones de RLS/ACL en sí.</summary>
    public string SuperuserConnectionString => _container.GetConnectionString();

    /// <summary>Autenticada como <c>procofa_app</c> — la que deben usar todas las queries que ejercen RLS/ACL de verdad.</summary>
    public string AppConnectionString
    {
        get
        {
            var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
            {
                Username = "procofa_app",
                Password = AppRolePassword,
            };
            return builder.ConnectionString;
        }
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        foreach (var script in new[] { "001_schema.sql", "002_security.sql", "003_seed_catalogs.sql" })
        {
            var sql = await ReadBaselineScriptAsync(script);

            await using var connection = new NpgsqlConnection(SuperuserConnectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 120;
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>Abre una conexión Npgsql como superusuario (bootstrap/semillas), ya abierta.</summary>
    public async Task<NpgsqlConnection> OpenSuperuserConnectionAsync()
    {
        var connection = new NpgsqlConnection(SuperuserConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    /// <summary>Abre una conexión Npgsql como <c>procofa_app</c> (para ejercer RLS/ACL de verdad), ya abierta.</summary>
    public async Task<NpgsqlConnection> OpenAppConnectionAsync()
    {
        var connection = new NpgsqlConnection(AppConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    /// <summary>
    /// Construye un <see cref="ProcofaDbContext"/> apuntando a este
    /// contenedor — con el mismo <see cref="ConcurrencyTokenInterceptor"/>
    /// que registra <c>DependencyInjection.AddInfrastructure</c>, para que
    /// los tests que pasan por EF ejerzan exactamente el mismo pipeline que
    /// la aplicación real.
    /// </summary>
    public ProcofaDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ProcofaDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(new ConcurrencyTokenInterceptor())
            .Options;

        return new ProcofaDbContext(options);
    }

    /// <summary>
    /// Crea un tenant nuevo (GUID + slug únicos) vía la conexión de
    /// superusuario — punto de partida de aislamiento para cada test.
    /// </summary>
    public async Task<Guid> CreateTenantAsync(string slugSuffix)
    {
        var tenantId = Guid.NewGuid();
        var slug = $"test-{slugSuffix}-{tenantId:N}"[..Math.Min(80, $"test-{slugSuffix}-{tenantId:N}".Length)];

        await using var connection = await OpenSuperuserConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO public.tenants (id, name, slug, is_active)
            VALUES (@id, @name, @slug, true);
            """;
        command.Parameters.AddWithValue("id", tenantId);
        command.Parameters.AddWithValue("name", $"Tenant de prueba {slugSuffix}");
        command.Parameters.AddWithValue("slug", slug);
        await command.ExecuteNonQueryAsync();

        return tenantId;
    }

    /// <summary>
    /// Crea un usuario mínimo dentro de un tenant — necesario como FK en
    /// múltiples columnas <c>*_by_user_id</c>/<c>*_user_id</c>. Password hash
    /// es un placeholder de prueba (no se ejerce autenticación en estos tests).
    /// </summary>
    public async Task<Guid> CreateUserAsync(Guid tenantId, string emailLocalPart)
    {
        var userId = Guid.NewGuid();
        var email = $"{emailLocalPart}.{userId:N}@example-test.procofa.invalid";

        await using var connection = await OpenSuperuserConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO public.users (id, tenant_id, email, password_hash, first_name, last_name, is_active)
            VALUES (@id, @tenantId, @email, @passwordHash, @firstName, @lastName, true);
            """;
        command.Parameters.AddWithValue("id", userId);
        command.Parameters.AddWithValue("tenantId", tenantId);
        command.Parameters.AddWithValue("email", email);
        command.Parameters.AddWithValue("passwordHash", "test-only-not-a-real-hash");
        command.Parameters.AddWithValue("firstName", "Test");
        command.Parameters.AddWithValue("lastName", emailLocalPart);
        await command.ExecuteNonQueryAsync();

        return userId;
    }

    /// <summary>
    /// Crea un usuario con un hash de contraseña REAL (a diferencia de
    /// <see cref="CreateUserAsync"/>, que usa un placeholder) y le asigna los
    /// roles indicados por código — usado por
    /// <c>Procofa.IntegrationTests.Auth</c> (Instrucción 04) para tener un
    /// usuario que efectivamente pueda hacer login/ser encontrado con rol.
    /// </summary>
    public async Task<Guid> CreateUserWithPasswordAsync(
        Guid tenantId, string email, string passwordHash, params string[] roleCodes)
    {
        var userId = Guid.NewGuid();

        await using var connection = await OpenSuperuserConnectionAsync();
        await ExecuteNonQueryAsync(connection, """
            INSERT INTO public.users (id, tenant_id, email, password_hash, first_name, last_name, is_active)
            VALUES (@id, @tenantId, @email, @passwordHash, 'Test', 'User', true);
            """, cmd =>
        {
            cmd.Parameters.AddWithValue("id", userId);
            cmd.Parameters.AddWithValue("tenantId", tenantId);
            cmd.Parameters.AddWithValue("email", email);
            cmd.Parameters.AddWithValue("passwordHash", passwordHash);
        });

        foreach (var roleCode in roleCodes)
        {
            var roleId = await GetCatalogIdByCodeAsync("roles", roleCode);

            await ExecuteNonQueryAsync(connection, """
                INSERT INTO public.user_roles (tenant_id, user_id, role_id)
                VALUES (@tenantId, @userId, @roleId);
                """, cmd =>
            {
                cmd.Parameters.AddWithValue("tenantId", tenantId);
                cmd.Parameters.AddWithValue("userId", userId);
                cmd.Parameters.AddWithValue("roleId", roleId);
            });
        }

        return userId;
    }

    /// <summary>Crea un cliente mínimo dentro de un tenant (solo columnas NOT NULL sin default).</summary>
    public async Task<Guid> CreateClientAsync(Guid tenantId, string legalName)
    {
        var clientId = Guid.NewGuid();

        await using var connection = await OpenSuperuserConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO public.clients (id, tenant_id, legal_name, is_active)
            VALUES (@id, @tenantId, @legalName, true);
            """;
        command.Parameters.AddWithValue("id", clientId);
        command.Parameters.AddWithValue("tenantId", tenantId);
        command.Parameters.AddWithValue("legalName", legalName);
        await command.ExecuteNonQueryAsync();

        return clientId;
    }

    /// <summary>Crea una empresa auditada mínima dentro de un cliente/tenant.</summary>
    public async Task<Guid> CreateAuditedCompanyAsync(Guid tenantId, Guid clientId, string legalName)
    {
        var companyId = Guid.NewGuid();

        await using var connection = await OpenSuperuserConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO public.audited_companies (id, tenant_id, client_id, legal_name, is_active)
            VALUES (@id, @tenantId, @clientId, @legalName, true);
            """;
        command.Parameters.AddWithValue("id", companyId);
        command.Parameters.AddWithValue("tenantId", tenantId);
        command.Parameters.AddWithValue("clientId", clientId);
        command.Parameters.AddWithValue("legalName", legalName);
        await command.ExecuteNonQueryAsync();

        return companyId;
    }

    /// <summary>
    /// Busca el <c>id</c> de una fila de catálogo por su <c>code</c> — evita
    /// hardcodear los GUID de <c>003_seed_catalogs.sql</c> en los tests
    /// (más legible, y no se desincroniza si el seed cambia).
    /// </summary>
    public async Task<Guid> GetCatalogIdByCodeAsync(string tableName, string code)
    {
        await using var connection = await OpenSuperuserConnectionAsync();
        await using var command = connection.CreateCommand();
        // tableName es siempre un literal fijo pasado por el propio test (no
        // input externo) -- interpolación aceptable aquí, nunca con datos de
        // usuario.
        command.CommandText = $"SELECT id FROM public.{tableName} WHERE code = @code;";
        command.Parameters.AddWithValue("code", code);

        var result = await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException($"No existe {tableName}.code = '{code}' en el seed.");

        return (Guid)result;
    }

    /// <summary>
    /// Crea una auditoría mínima (folio único, status BORRADOR, modalidad
    /// ONSITE) junto con su cliente y empresa auditada — helper compartido
    /// por los tests que necesitan una <c>audits</c> válida como FK (varias
    /// tablas de Findings/Reports dependen de ella).
    /// </summary>
    public async Task<AuditFixtureData> CreateMinimalAuditAsync(Guid tenantId, Guid createdByUserId, string folioSuffix)
    {
        var clientId = await CreateClientAsync(tenantId, $"Cliente Audit {folioSuffix}");
        var companyId = await CreateAuditedCompanyAsync(tenantId, clientId, $"Empresa Audit {folioSuffix}");

        var auditTypeId = await GetCatalogIdByCodeAsync("audit_types", "INTERNA_OEA");
        var profileId = await GetCatalogIdByCodeAsync("profiles", "MAQUILA");
        var draftStatusId = await GetCatalogIdByCodeAsync("audit_statuses", "BORRADOR");

        var auditId = Guid.NewGuid();
        var folio = $"AUD-TEST-{folioSuffix}";

        await using var connection = await OpenSuperuserConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO public.audits (
                id, tenant_id, folio, client_id, audited_company_id, audit_type_id,
                profile_id, status_id, objective, scope, scheduled_date,
                created_by_user_id, execution_mode)
            VALUES (
                @id, @tenantId, @folio, @clientId, @companyId, @auditTypeId,
                @profileId, @statusId, @objective, @scope, CURRENT_DATE,
                @createdByUserId, 'ONSITE');
            """;
        command.Parameters.AddWithValue("id", auditId);
        command.Parameters.AddWithValue("tenantId", tenantId);
        command.Parameters.AddWithValue("folio", folio);
        command.Parameters.AddWithValue("clientId", clientId);
        command.Parameters.AddWithValue("companyId", companyId);
        command.Parameters.AddWithValue("auditTypeId", auditTypeId);
        command.Parameters.AddWithValue("profileId", profileId);
        command.Parameters.AddWithValue("statusId", draftStatusId);
        command.Parameters.AddWithValue("objective", "Objetivo de prueba de integración.");
        command.Parameters.AddWithValue("scope", "Alcance de prueba de integración.");
        command.Parameters.AddWithValue("createdByUserId", createdByUserId);
        await command.ExecuteNonQueryAsync();

        return new AuditFixtureData(auditId, clientId, companyId, draftStatusId);
    }

    /// <summary>
    /// Crea la cadena completa <c>checklists → checklist_versions →
    /// checklist_sections → criteria → audit_checklists → audit_criteria</c>
    /// necesaria para tener UN <c>audit_criteria</c> real y válido — usada por
    /// <c>AuditCloseValidationTests</c> (controla <paramref name="complianceStatusCode"/>
    /// para simular criterio evaluado/sin evaluar) y <c>ConcurrencyTokenTests</c>
    /// (necesita una fila real con <c>lock_version</c> para el UPDATE
    /// concurrente vía EF). Fidelidad sobre atajos (Instrucción 03): NO se usa
    /// ninguna tabla intermedia falsa, se recorre la cadena de FKs real completa.
    /// <c>checklists.program_id</c>/<c>profile_id</c> usan catálogos fijos
    /// ('OEA'/'MAQUILA') — suficientes para satisfacer las FKs, no relevantes
    /// para lo que el test valida.
    /// </summary>
    public async Task<Guid> CreateAuditCriterionAsync(
        Guid tenantId,
        Guid auditId,
        Guid createdByUserId,
        bool isMandatorySnapshot,
        string? complianceStatusCode,
        string suffix)
    {
        var programId = await GetCatalogIdByCodeAsync("programs", "OEA");
        var profileId = await GetCatalogIdByCodeAsync("profiles", "MAQUILA");
        Guid? complianceStatusId = complianceStatusCode is null
            ? null
            : await GetCatalogIdByCodeAsync("compliance_statuses", complianceStatusCode);

        await using var connection = await OpenSuperuserConnectionAsync();

        var checklistId = Guid.NewGuid();
        await ExecuteNonQueryAsync(connection, """
            INSERT INTO public.checklists (id, tenant_id, program_id, profile_id, name, created_by_user_id)
            VALUES (@id, @tenantId, @programId, @profileId, @name, @createdBy);
            """, cmd =>
        {
            cmd.Parameters.AddWithValue("id", checklistId);
            cmd.Parameters.AddWithValue("tenantId", tenantId);
            cmd.Parameters.AddWithValue("programId", programId);
            cmd.Parameters.AddWithValue("profileId", profileId);
            cmd.Parameters.AddWithValue("name", $"Checklist {suffix}");
            cmd.Parameters.AddWithValue("createdBy", createdByUserId);
        });

        var checklistVersionId = Guid.NewGuid();
        await ExecuteNonQueryAsync(connection, """
            INSERT INTO public.checklist_versions (id, tenant_id, checklist_id, version_number, created_by_user_id)
            VALUES (@id, @tenantId, @checklistId, 1, @createdBy);
            """, cmd =>
        {
            cmd.Parameters.AddWithValue("id", checklistVersionId);
            cmd.Parameters.AddWithValue("tenantId", tenantId);
            cmd.Parameters.AddWithValue("checklistId", checklistId);
            cmd.Parameters.AddWithValue("createdBy", createdByUserId);
        });

        var sectionId = Guid.NewGuid();
        await ExecuteNonQueryAsync(connection, """
            INSERT INTO public.checklist_sections (id, tenant_id, checklist_version_id, name)
            VALUES (@id, @tenantId, @checklistVersionId, @name);
            """, cmd =>
        {
            cmd.Parameters.AddWithValue("id", sectionId);
            cmd.Parameters.AddWithValue("tenantId", tenantId);
            cmd.Parameters.AddWithValue("checklistVersionId", checklistVersionId);
            cmd.Parameters.AddWithValue("name", $"Sección {suffix}");
        });

        var criterionId = Guid.NewGuid();
        var criterionCode = $"CRIT-{suffix}";
        var question = $"¿Pregunta de prueba {suffix}?";
        await ExecuteNonQueryAsync(connection, """
            INSERT INTO public.criteria (id, tenant_id, checklist_section_id, code, audit_question)
            VALUES (@id, @tenantId, @sectionId, @code, @question);
            """, cmd =>
        {
            cmd.Parameters.AddWithValue("id", criterionId);
            cmd.Parameters.AddWithValue("tenantId", tenantId);
            cmd.Parameters.AddWithValue("sectionId", sectionId);
            cmd.Parameters.AddWithValue("code", criterionCode);
            cmd.Parameters.AddWithValue("question", question);
        });

        var auditChecklistId = Guid.NewGuid();
        await ExecuteNonQueryAsync(connection, """
            INSERT INTO public.audit_checklists (id, tenant_id, audit_id, checklist_version_id)
            VALUES (@id, @tenantId, @auditId, @checklistVersionId);
            """, cmd =>
        {
            cmd.Parameters.AddWithValue("id", auditChecklistId);
            cmd.Parameters.AddWithValue("tenantId", tenantId);
            cmd.Parameters.AddWithValue("auditId", auditId);
            cmd.Parameters.AddWithValue("checklistVersionId", checklistVersionId);
        });

        var auditCriterionId = Guid.NewGuid();
        await ExecuteNonQueryAsync(connection, """
            INSERT INTO public.audit_criteria (
                id, tenant_id, audit_id, audit_checklist_id, criterion_id, compliance_status_id,
                criterion_code_snapshot, question_snapshot, is_mandatory_snapshot)
            VALUES (
                @id, @tenantId, @auditId, @auditChecklistId, @criterionId, @complianceStatusId,
                @criterionCode, @question, @isMandatory);
            """, cmd =>
        {
            cmd.Parameters.AddWithValue("id", auditCriterionId);
            cmd.Parameters.AddWithValue("tenantId", tenantId);
            cmd.Parameters.AddWithValue("auditId", auditId);
            cmd.Parameters.AddWithValue("auditChecklistId", auditChecklistId);
            cmd.Parameters.AddWithValue("criterionId", criterionId);
            cmd.Parameters.AddWithValue("complianceStatusId", (object?)complianceStatusId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("criterionCode", criterionCode);
            cmd.Parameters.AddWithValue("question", question);
            cmd.Parameters.AddWithValue("isMandatory", isMandatorySnapshot);
        });

        return auditCriterionId;
    }

    /// <summary>
    /// Crea un <c>audit_reports</c> mínimo con el <c>status</c> indicado —
    /// usada por <c>AppendOnlyAndImmutabilityTests</c> para crear directamente
    /// un reporte en <c>FINAL</c> (se salta el flujo normal DRAFT→FINAL a
    /// propósito: el test ejercita el trigger de inmutabilidad de base de
    /// datos, no el flujo de negocio, que Instrucción 03 excluye).
    /// </summary>
    public async Task<Guid> CreateAuditReportAsync(
        Guid tenantId,
        Guid auditId,
        Guid generatedByUserId,
        string status,
        string reportType = "FINAL")
    {
        var reportId = Guid.NewGuid();

        await using var connection = await OpenSuperuserConnectionAsync();
        await ExecuteNonQueryAsync(connection, """
            INSERT INTO public.audit_reports (
                id, tenant_id, audit_id, report_type, format, status, storage_key, generated_by_user_id)
            VALUES (@id, @tenantId, @auditId, @reportType, 'PDF', @status, @storageKey, @generatedBy);
            """, cmd =>
        {
            cmd.Parameters.AddWithValue("id", reportId);
            cmd.Parameters.AddWithValue("tenantId", tenantId);
            cmd.Parameters.AddWithValue("auditId", auditId);
            cmd.Parameters.AddWithValue("reportType", reportType);
            cmd.Parameters.AddWithValue("status", status);
            cmd.Parameters.AddWithValue("storageKey", $"test/reports/{reportId:N}.pdf");
            cmd.Parameters.AddWithValue("generatedBy", generatedByUserId);
        });

        return reportId;
    }

    /// <summary>Crea un <c>audit_logs</c> mínimo — usada por <c>AppendOnlyAndImmutabilityTests</c>.</summary>
    public async Task<Guid> CreateAuditLogAsync(Guid tenantId, string entityName, string action)
    {
        var logId = Guid.NewGuid();

        await using var connection = await OpenSuperuserConnectionAsync();
        await ExecuteNonQueryAsync(connection, """
            INSERT INTO public.audit_logs (id, tenant_id, entity_name, action)
            VALUES (@id, @tenantId, @entityName, @action);
            """, cmd =>
        {
            cmd.Parameters.AddWithValue("id", logId);
            cmd.Parameters.AddWithValue("tenantId", tenantId);
            cmd.Parameters.AddWithValue("entityName", entityName);
            cmd.Parameters.AddWithValue("action", action);
        });

        return logId;
    }

    private static async Task ExecuteNonQueryAsync(
        NpgsqlConnection connection, string sql, Action<NpgsqlCommand> configureParameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        configureParameters(command);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> ReadBaselineScriptAsync(string fileName)
    {
        // db/baseline/v2.1/*.sql vive en la raíz del repo. En tiempo de
        // ejecución de los tests, AppContext.BaseDirectory apunta a
        // bin/Debug/net10.0/ dentro de tests/Procofa.IntegrationTests/ —
        // se prueban ambas rutas (relativa al repo y relativa a la salida
        // de build, por si el .csproj copia estos archivos al output) para
        // no depender de un único mecanismo.
        string[] candidatePaths =
        [
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "db", "baseline", "v2.1", fileName),
            Path.Combine(AppContext.BaseDirectory, "db", "baseline", "v2.1", fileName),
        ];

        foreach (var candidate in candidatePaths)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(fullPath))
            {
                return await File.ReadAllTextAsync(fullPath);
            }
        }

        throw new FileNotFoundException(
            $"No se encontró db/baseline/v2.1/{fileName} desde ninguna de las rutas candidatas. " +
            "Verifique que el archivo exista en la raíz del repo, o agregue una regla " +
            "'Copy to Output Directory' en Procofa.IntegrationTests.csproj.");
    }
}

/// <summary>
/// Datos mínimos de una auditoría creada por <see cref="PostgresBaselineFixture.CreateMinimalAuditAsync"/>
/// — devueltos como un solo valor para que los tests que necesitan una
/// <c>audits</c> válida (Findings/Reports) no tengan que repetir las mismas
/// cuatro consultas de resolución de IDs.
/// </summary>
/// <param name="AuditId">Id de la fila insertada en <c>public.audits</c>.</param>
/// <param name="ClientId">Id del <c>clients</c> creado como dueño de la auditoría.</param>
/// <param name="CompanyId">Id del <c>audited_companies</c> auditado.</param>
/// <param name="StatusId">Id de <c>audit_statuses</c> con <c>code = 'BORRADOR'</c> (estado inicial).</param>
public sealed record AuditFixtureData(Guid AuditId, Guid ClientId, Guid CompanyId, Guid StatusId);
