using Procofa.Domain.Enums;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Tests;

/// <summary>
/// Prueba los 16 converters explícitos de <c>Persistence/Conversions/Enums/</c>
/// (Instrucción 03.1, defecto 1) contra el contrato físico exacto documentado
/// en cada enum de Domain — nunca contra <c>Enum.ToString()</c> ni contra
/// transformación automática de mayúsculas/PascalCase→snake_case. Cada test
/// ejercita el converter a través de la API pública real de
/// <c>ValueConverter&lt;TModel,TProvider&gt;</c> (<c>ConvertToProvider</c>/
/// <c>ConvertFromProvider</c>) — el mismo camino que EF Core usa en runtime —
/// en vez de invocar métodos internos, para que el test valide el contrato
/// que efectivamente ve la base de datos.
///
/// Cobertura completa (round-trip) para los 16 enums VARCHAR+CHECK del
/// baseline V2.1; cobertura reforzada (valor físico inválido debe lanzar)
/// para los 4 explícitamente pedidos: <see cref="ExecutionMode"/>,
/// <see cref="AccessLogEventType"/>, <see cref="SignerType"/> e
/// <see cref="IdempotencyOperationStatus"/>.
/// </summary>
public sealed class EnumConverterTests
{
    // ---- Cobertura reforzada: los 4 converters explícitamente pedidos ----

    [Theory]
    [InlineData(ExecutionMode.Onsite, "ONSITE")]
    [InlineData(ExecutionMode.Remote, "REMOTE")]
    [InlineData(ExecutionMode.Hybrid, "HYBRID")]
    public void ExecutionModeConverter_RoundTrip_EsExplicitoYExacto(ExecutionMode enumValue, string dbValue)
    {
        var converter = new ExecutionModeConverter();

        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Fact]
    public void ExecutionModeConverter_ValorFisicoDesconocido_Lanza()
    {
        var converter = new ExecutionModeConverter();

        Assert.Throws<ArgumentOutOfRangeException>(() => converter.ConvertFromProvider("REMOTO"));
    }

    [Theory]
    [InlineData(AccessLogEventType.LoginSuccess, "LOGIN_SUCCESS")]
    [InlineData(AccessLogEventType.LoginFailure, "LOGIN_FAILURE")]
    [InlineData(AccessLogEventType.Logout, "LOGOUT")]
    [InlineData(AccessLogEventType.PasswordResetRequest, "PASSWORD_RESET_REQUEST")]
    [InlineData(AccessLogEventType.PasswordResetSuccess, "PASSWORD_RESET_SUCCESS")]
    [InlineData(AccessLogEventType.AccountLocked, "ACCOUNT_LOCKED")]
    public void AccessLogEventTypeConverter_RoundTrip_EsExplicitoYExacto(AccessLogEventType enumValue, string dbValue)
    {
        var converter = new AccessLogEventTypeConverter();

        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Fact]
    public void AccessLogEventTypeConverter_ValorFisicoDesconocido_Lanza()
    {
        var converter = new AccessLogEventTypeConverter();

        Assert.Throws<ArgumentOutOfRangeException>(() => converter.ConvertFromProvider("LOGIN_OK"));
    }

    [Theory]
    [InlineData(SignerType.AuditorLider, "AUDITOR_LIDER")]
    [InlineData(SignerType.Auditor, "AUDITOR")]
    [InlineData(SignerType.Cliente, "CLIENTE")]
    [InlineData(SignerType.Responsable, "RESPONSABLE")]
    public void SignerTypeConverter_RoundTrip_EsExplicitoYExacto(SignerType enumValue, string dbValue)
    {
        var converter = new SignerTypeConverter();

        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Fact]
    public void SignerTypeConverter_ValorFisicoDesconocido_Lanza()
    {
        var converter = new SignerTypeConverter();

        // Prueba explícita de que NO se acepta transformación automática
        // PascalCase->snake_case: "AuditorLider" en mayúsculas simples NO es
        // el contrato físico real ("AUDITOR_LIDER").
        Assert.Throws<ArgumentOutOfRangeException>(() => converter.ConvertFromProvider("AUDITORLIDER"));
    }

    [Theory]
    [InlineData(IdempotencyOperationStatus.InProgress, "IN_PROGRESS")]
    [InlineData(IdempotencyOperationStatus.Completed, "COMPLETED")]
    [InlineData(IdempotencyOperationStatus.Failed, "FAILED")]
    public void IdempotencyOperationStatusConverter_RoundTrip_EsExplicitoYExacto(
        IdempotencyOperationStatus enumValue, string dbValue)
    {
        var converter = new IdempotencyOperationStatusConverter();

        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Fact]
    public void IdempotencyOperationStatusConverter_ValorFisicoDesconocido_Lanza()
    {
        var converter = new IdempotencyOperationStatusConverter();

        // "INPROGRESS" (sin guión bajo) sería lo que produciría un
        // Enum.ToString().ToUpper() ingenuo -- debe fallar igual que
        // cualquier otro valor no contemplado en el contrato explícito.
        Assert.Throws<ArgumentOutOfRangeException>(() => converter.ConvertFromProvider("INPROGRESS"));
    }

    // ---- Cobertura de round-trip para los 12 converters restantes ----

    [Theory]
    [InlineData(AuditReportStatus.Draft, "DRAFT")]
    [InlineData(AuditReportStatus.Final, "FINAL")]
    [InlineData(AuditReportStatus.Void, "VOID")]
    public void AuditReportStatusConverter_RoundTrip(AuditReportStatus enumValue, string dbValue)
    {
        var converter = new AuditReportStatusConverter();
        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Theory]
    [InlineData(AuditTeamRole.Lead, "LEAD")]
    [InlineData(AuditTeamRole.Support, "SUPPORT")]
    public void AuditTeamRoleConverter_RoundTrip(AuditTeamRole enumValue, string dbValue)
    {
        var converter = new AuditTeamRoleConverter();
        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Theory]
    [InlineData(ChecklistVersionStatus.Draft, "DRAFT")]
    [InlineData(ChecklistVersionStatus.Published, "PUBLISHED")]
    [InlineData(ChecklistVersionStatus.Retired, "RETIRED")]
    public void ChecklistVersionStatusConverter_RoundTrip(ChecklistVersionStatus enumValue, string dbValue)
    {
        var converter = new ChecklistVersionStatusConverter();
        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Theory]
    [InlineData(DocumentRequestStatus.Pendiente, "PENDIENTE")]
    [InlineData(DocumentRequestStatus.Entregado, "ENTREGADO")]
    [InlineData(DocumentRequestStatus.Validado, "VALIDADO")]
    [InlineData(DocumentRequestStatus.Rechazado, "RECHAZADO")]
    [InlineData(DocumentRequestStatus.Cancelado, "CANCELADO")]
    public void DocumentRequestStatusConverter_RoundTrip(DocumentRequestStatus enumValue, string dbValue)
    {
        var converter = new DocumentRequestStatusConverter();
        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Theory]
    [InlineData(EvidenceType.Foto, "FOTO")]
    [InlineData(EvidenceType.Pdf, "PDF")]
    [InlineData(EvidenceType.Word, "WORD")]
    [InlineData(EvidenceType.Excel, "EXCEL")]
    [InlineData(EvidenceType.Imagen, "IMAGEN")]
    [InlineData(EvidenceType.Captura, "CAPTURA")]
    [InlineData(EvidenceType.Registro, "REGISTRO")]
    [InlineData(EvidenceType.Otro, "OTRO")]
    public void EvidenceTypeConverter_RoundTrip(EvidenceType enumValue, string dbValue)
    {
        var converter = new EvidenceTypeConverter();
        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(ImportanceLevel.Alta, "ALTA")]
    [InlineData(ImportanceLevel.Media, "MEDIA")]
    [InlineData(ImportanceLevel.Baja, "BAJA")]
    public void ImportanceLevelConverter_RoundTrip_IncluyendoNull(ImportanceLevel? enumValue, string? dbValue)
    {
        var converter = new ImportanceLevelConverter();
        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Theory]
    [InlineData(NotificationChannel.Internal, "INTERNAL")]
    [InlineData(NotificationChannel.Email, "EMAIL")]
    public void NotificationChannelConverter_RoundTrip(NotificationChannel enumValue, string dbValue)
    {
        var converter = new NotificationChannelConverter();
        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Theory]
    [InlineData(ObservationType.Auditor, "AUDITOR")]
    [InlineData(ObservationType.Cliente, "CLIENTE")]
    [InlineData(ObservationType.Interna, "INTERNA")]
    public void ObservationTypeConverter_RoundTrip(ObservationType enumValue, string dbValue)
    {
        var converter = new ObservationTypeConverter();
        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Theory]
    [InlineData(OutboxMessageStatus.Pending, "PENDING")]
    [InlineData(OutboxMessageStatus.Processing, "PROCESSING")]
    [InlineData(OutboxMessageStatus.Processed, "PROCESSED")]
    [InlineData(OutboxMessageStatus.Failed, "FAILED")]
    public void OutboxMessageStatusConverter_RoundTrip(OutboxMessageStatus enumValue, string dbValue)
    {
        var converter = new OutboxMessageStatusConverter();
        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Theory]
    [InlineData(ReportFormat.Pdf, "PDF")]
    [InlineData(ReportFormat.Docx, "DOCX")]
    [InlineData(ReportFormat.Xlsx, "XLSX")]
    public void ReportFormatConverter_RoundTrip(ReportFormat enumValue, string dbValue)
    {
        var converter = new ReportFormatConverter();
        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Theory]
    [InlineData(ReportTemplateVersionStatus.Draft, "DRAFT")]
    [InlineData(ReportTemplateVersionStatus.Published, "PUBLISHED")]
    [InlineData(ReportTemplateVersionStatus.Retired, "RETIRED")]
    public void ReportTemplateVersionStatusConverter_RoundTrip(ReportTemplateVersionStatus enumValue, string dbValue)
    {
        var converter = new ReportTemplateVersionStatusConverter();
        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    [Theory]
    [InlineData(ReportType.Final, "FINAL")]
    [InlineData(ReportType.Ejecutivo, "EJECUTIVO")]
    [InlineData(ReportType.Hallazgos, "HALLAZGOS")]
    [InlineData(ReportType.Acciones, "ACCIONES")]
    [InlineData(ReportType.Seguimiento, "SEGUIMIENTO")]
    public void ReportTypeConverter_RoundTrip(ReportType enumValue, string dbValue)
    {
        var converter = new ReportTypeConverter();
        Assert.Equal(dbValue, converter.ConvertToProvider(enumValue));
        Assert.Equal(enumValue, converter.ConvertFromProvider(dbValue));
    }

    // ---- Chequeo estructural: los 16 converters existen y son sealed ----

    [Fact]
    public void Existen16ConvertersExplicitos_UnoPorCadaEnumVarcharCheckDelBaseline()
    {
        var converterTypes = new[]
        {
            typeof(AccessLogEventTypeConverter),
            typeof(AuditReportStatusConverter),
            typeof(AuditTeamRoleConverter),
            typeof(ChecklistVersionStatusConverter),
            typeof(DocumentRequestStatusConverter),
            typeof(EvidenceTypeConverter),
            typeof(ExecutionModeConverter),
            typeof(IdempotencyOperationStatusConverter),
            typeof(ImportanceLevelConverter),
            typeof(NotificationChannelConverter),
            typeof(ObservationTypeConverter),
            typeof(OutboxMessageStatusConverter),
            typeof(ReportFormatConverter),
            typeof(ReportTemplateVersionStatusConverter),
            typeof(ReportTypeConverter),
            typeof(SignerTypeConverter),
        };

        Assert.Equal(16, converterTypes.Length);
        Assert.All(converterTypes, t => Assert.True(t.IsSealed));
    }
}
