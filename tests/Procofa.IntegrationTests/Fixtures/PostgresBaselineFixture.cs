using Microsoft.EntityFrameworkCore;
using Npgsql;
using Procofa.Infrastructure.Persistence;
using Procofa.Infrastructure.Persistence.Interceptors;
using Testcontainers.PostgreSql;

namespace Procofa.IntegrationTests.Fixtures;


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

    
    public string SuperuserConnectionString => _container.GetConnectionString();

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

   
    public async Task<NpgsqlConnection> OpenSuperuserConnectionAsync()
    {
        var connection = new NpgsqlConnection(SuperuserConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<NpgsqlConnection> OpenAppConnectionAsync()
    {
        var connection = new NpgsqlConnection(AppConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public ProcofaDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ProcofaDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(new ConcurrencyTokenInterceptor())
            .Options;

        return new ProcofaDbContext(options);
    }

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

    public async Task<Guid> GetCatalogIdByCodeAsync(string tableName, string code)
    {
        await using var connection = await OpenSuperuserConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = $"SELECT id FROM public.{tableName} WHERE code = @code;";
        command.Parameters.AddWithValue("code", code);

        var result = await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException($"No existe {tableName}.code = '{code}' en el seed.");

        return (Guid)result;
    }


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


public sealed record AuditFixtureData(Guid AuditId, Guid ClientId, Guid CompanyId, Guid StatusId);
