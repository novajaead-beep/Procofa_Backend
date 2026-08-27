using Procofa.Application.Abstractions;

namespace Procofa.IntegrationTests.Users;

/// <summary>
/// <see cref="ICurrentUser"/> de prueba para integration tests (Instrucción
/// 05) — equivalente al <c>FakeCurrentUser</c> de Procofa.Application.Tests,
/// duplicado aquí porque ese es <c>internal</c> a su propio ensamblado. La
/// implementación HTTP real (<c>HttpContextCurrentUser</c>) vive en Api.
/// </summary>
internal sealed class StaticCurrentUser(Guid userId) : ICurrentUser
{
    public Guid UserId { get; } = userId;
}
