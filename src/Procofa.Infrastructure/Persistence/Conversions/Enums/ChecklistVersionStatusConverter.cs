using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="ChecklistVersionStatus"/> ↔
/// <c>checklist_versions.status varchar(20)</c> (Instrucción 03.1, defecto 1).
/// </summary>
public sealed class ChecklistVersionStatusConverter : ValueConverter<ChecklistVersionStatus, string>
{
    public ChecklistVersionStatusConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(ChecklistVersionStatus value) => value switch
    {
        ChecklistVersionStatus.Draft => "DRAFT",
        ChecklistVersionStatus.Published => "PUBLISHED",
        ChecklistVersionStatus.Retired => "RETIRED",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(ChecklistVersionStatus)} sin mapeo físico explícito."),
    };

    private static ChecklistVersionStatus FromDb(string value) => value switch
    {
        "DRAFT" => ChecklistVersionStatus.Draft,
        "PUBLISHED" => ChecklistVersionStatus.Published,
        "RETIRED" => ChecklistVersionStatus.Retired,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(ChecklistVersionStatus)}."),
    };
}
