namespace Procofa.Application.UseCases.Auth.BootstrapAdmin;

/// <summary>
/// Comando de entrada del bootstrap one-shot del primer ADMIN (Instrucción
/// 04, sección "BOOTSTRAP PRIMER ADMIN"). Recibido desde variables de
/// entorno / línea de comandos por <c>Procofa.Api</c> (host mode
/// <c>bootstrap-admin</c>) — nunca desde un endpoint HTTP público.
/// </summary>
public sealed record BootstrapAdminCommand(string Email, string Password, string FirstName, string LastName);
