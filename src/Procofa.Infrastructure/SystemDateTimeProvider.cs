using Procofa.Application.Abstractions;

namespace Procofa.Infrastructure;

/// <summary>Implementación real de <see cref="IDateTimeProvider"/> — el único punto del proceso que llama <see cref="DateTime.UtcNow"/> para lógica de negocio.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
