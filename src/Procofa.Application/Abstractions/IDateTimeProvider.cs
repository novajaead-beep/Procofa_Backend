namespace Procofa.Application.Abstractions;

/// <summary>
/// Puerto de reloj (Instrucción 04): Application nunca llama
/// <c>DateTime.UtcNow</c> directamente — todo instante "ahora" usado en
/// lógica de negocio (lockout, expiración de tokens) pasa por este puerto,
/// para que los tests de Application puedan controlar el tiempo con un fake
/// determinista sin esperar relojes reales.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
