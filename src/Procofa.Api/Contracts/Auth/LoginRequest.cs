namespace Procofa.Api.Contracts.Auth;

/// <summary>Body de <c>POST /api/auth/login</c>. Ambos campos nullable a propósito — la validación de "obligatorio" ocurre explícitamente en el endpoint, nunca vía excepción de binding.</summary>
public sealed record LoginRequest(string? Email, string? Password);
