using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Procofa.Infrastructure.Persistence.Conversions;

/// <summary>
/// Normaliza cualquier <see cref="DateTime"/> a <see cref="DateTimeKind.Utc"/>
/// antes de escribirlo a Postgres, y vuelve a marcar como <c>Utc</c> lo leído
/// de vuelta. Estrategia UTC única y centralizada para todas las columnas
/// <c>timestamptz</c> del modelo (Instrucción 03: "definir y justificar una
/// estrategia UTC consistente").
///
/// Justificación: Npgsql 6+ exige que todo <see cref="DateTime"/> escrito en
/// una columna <c>timestamptz</c> tenga <c>Kind = Utc</c> — lanza en runtime
/// ante <c>Kind = Unspecified</c> o <c>Kind = Local</c>. Sin este converter,
/// cualquier código de Application/Domain que construya un
/// <see cref="DateTime"/> sin fijar explícitamente <c>Kind = Utc</c> (p. ej.
/// <c>new DateTime(2026, 1, 1)</c>, <c>Kind = Unspecified</c>) rompería en
/// runtime al primer <c>SaveChanges</c>. <c>Unspecified</c> se asume
/// ya-en-UTC y solo se re-etiqueta; <c>Local</c> se convierte de verdad vía
/// <see cref="DateTime.ToUniversalTime"/>.
///
/// Aplicado globalmente vía <c>ConfigureConventions</c> en
/// <c>ProcofaDbContext</c> — ninguna <c>IEntityTypeConfiguration&lt;T&gt;</c>
/// individual repite esta lógica columna por columna.
///
/// La lógica vive en un método estático normal (<see cref="ToUtc"/>) en vez
/// de directamente en la expresión lambda del converter porque un
/// <c>switch</c> — expresión no es válido dentro de un
/// <c>Expression&lt;Func&lt;...&gt;&gt;</c> (CS8143); llamar a un método
/// desde la lambda sí lo es.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => ToUtc(v),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }

    internal static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}

/// <summary>Variante nullable de <see cref="UtcDateTimeConverter"/> — misma estrategia.</summary>
public sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            v => v.HasValue ? UtcDateTimeConverter.ToUtc(v.Value) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {
    }
}
