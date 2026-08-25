using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="EvidenceType"/> ↔
/// <c>audit_evidences.evidence_type varchar(30)</c> (Instrucción 03.1, defecto 1).
/// </summary>
public sealed class EvidenceTypeConverter : ValueConverter<EvidenceType, string>
{
    public EvidenceTypeConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(EvidenceType value) => value switch
    {
        EvidenceType.Foto => "FOTO",
        EvidenceType.Pdf => "PDF",
        EvidenceType.Word => "WORD",
        EvidenceType.Excel => "EXCEL",
        EvidenceType.Imagen => "IMAGEN",
        EvidenceType.Captura => "CAPTURA",
        EvidenceType.Registro => "REGISTRO",
        EvidenceType.Otro => "OTRO",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(EvidenceType)} sin mapeo físico explícito."),
    };

    private static EvidenceType FromDb(string value) => value switch
    {
        "FOTO" => EvidenceType.Foto,
        "PDF" => EvidenceType.Pdf,
        "WORD" => EvidenceType.Word,
        "EXCEL" => EvidenceType.Excel,
        "IMAGEN" => EvidenceType.Imagen,
        "CAPTURA" => EvidenceType.Captura,
        "REGISTRO" => EvidenceType.Registro,
        "OTRO" => EvidenceType.Otro,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(EvidenceType)}."),
    };
}
