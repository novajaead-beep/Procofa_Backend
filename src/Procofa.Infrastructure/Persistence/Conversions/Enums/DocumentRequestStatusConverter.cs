using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="DocumentRequestStatus"/> ↔
/// <c>audit_document_requests.status varchar(30)</c> (Instrucción 03.1, defecto 1).
/// </summary>
public sealed class DocumentRequestStatusConverter : ValueConverter<DocumentRequestStatus, string>
{
    public DocumentRequestStatusConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(DocumentRequestStatus value) => value switch
    {
        DocumentRequestStatus.Pendiente => "PENDIENTE",
        DocumentRequestStatus.Entregado => "ENTREGADO",
        DocumentRequestStatus.Validado => "VALIDADO",
        DocumentRequestStatus.Rechazado => "RECHAZADO",
        DocumentRequestStatus.Cancelado => "CANCELADO",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(DocumentRequestStatus)} sin mapeo físico explícito."),
    };

    private static DocumentRequestStatus FromDb(string value) => value switch
    {
        "PENDIENTE" => DocumentRequestStatus.Pendiente,
        "ENTREGADO" => DocumentRequestStatus.Entregado,
        "VALIDADO" => DocumentRequestStatus.Validado,
        "RECHAZADO" => DocumentRequestStatus.Rechazado,
        "CANCELADO" => DocumentRequestStatus.Cancelado,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(DocumentRequestStatus)}."),
    };
}
