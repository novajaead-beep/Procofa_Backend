using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="SignerType"/> ↔
/// <c>audit_signatories.signer_type varchar(30)</c> (Instrucción 03.1, defecto 1).
/// </summary>
public sealed class SignerTypeConverter : ValueConverter<SignerType, string>
{
    public SignerTypeConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(SignerType value) => value switch
    {
        SignerType.AuditorLider => "AUDITOR_LIDER",
        SignerType.Auditor => "AUDITOR",
        SignerType.Cliente => "CLIENTE",
        SignerType.Responsable => "RESPONSABLE",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(SignerType)} sin mapeo físico explícito."),
    };

    private static SignerType FromDb(string value) => value switch
    {
        "AUDITOR_LIDER" => SignerType.AuditorLider,
        "AUDITOR" => SignerType.Auditor,
        "CLIENTE" => SignerType.Cliente,
        "RESPONSABLE" => SignerType.Responsable,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(SignerType)}."),
    };
}
