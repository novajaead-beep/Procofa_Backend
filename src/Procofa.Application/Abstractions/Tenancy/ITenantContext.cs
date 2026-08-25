namespace Procofa.Application.Abstractions.Tenancy;

/// <summary>
/// Representa el tenant efectivo de la operación en curso (Instrucción 03,
/// sección 24-25). Puerto puro de Application: no depende de EF Core,
/// Npgsql ni HttpContext — solo de <see cref="System.Guid"/>.
/// </summary>
/// <remarks>
/// Resolución esperada según el punto de entrada (sección 24 de la
/// instrucción y sección I del baseline):
/// <list type="bullet">
/// <item>Login / refresh token: resuelto desde configuración segura antes de
/// tocar la BD (Etapa 1 = GUID fijo de PROCOFA,
/// <c>00000000-0000-0000-0000-000000000001</c>).</item>
/// <item>Request autenticado: resuelto desde el claim <c>tenant_id</c> del
/// JWT (Auth, instrucción futura).</item>
/// <item>Background job: resuelto desde el payload del mensaje
/// (<c>outbox_messages.tenant_id</c>).</item>
/// </list>
/// El frontend nunca decide el tenant efectivo (sección 21 del handoff) — la
/// implementación HTTP de este puerto no debe leer un <c>tenantId</c> del
/// body/query de la request.
/// </remarks>
public interface ITenantContext
{
    /// <summary>
    /// Tenant efectivo de la operación en curso. No nullable: todo código que
    /// necesita un <see cref="ITenantContext"/> debe tener el tenant ya
    /// resuelto — no hay una operación tenant-scoped válida sin él.
    /// </summary>
    Guid TenantId { get; }
}
