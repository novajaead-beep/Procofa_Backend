using Procofa.Application.Abstractions.Identity;

namespace Procofa.Infrastructure.Security;

/// <summary>Implementación de <see cref="IAuthPolicyOptions"/> a partir de los primitivos de <see cref="InfrastructureAuthSettings"/>.</summary>
public sealed class AuthPolicyOptionsAdapter : IAuthPolicyOptions
{
    public AuthPolicyOptionsAdapter(InfrastructureAuthSettings settings)
    {
        if (settings.AuthMaxFailedLoginAttempts <= 0)
        {
            throw new InvalidOperationException("Auth:MaxFailedLoginAttempts debe ser mayor a 0.");
        }

        if (settings.AuthLockoutMinutes <= 0)
        {
            throw new InvalidOperationException("Auth:LockoutMinutes debe ser mayor a 0.");
        }

        if (settings.AuthRefreshTokenDays <= 0)
        {
            throw new InvalidOperationException("Auth:RefreshTokenDays debe ser mayor a 0.");
        }

        MaxFailedLoginAttempts = settings.AuthMaxFailedLoginAttempts;
        LockoutDuration = TimeSpan.FromMinutes(settings.AuthLockoutMinutes);
        RefreshTokenLifetime = TimeSpan.FromDays(settings.AuthRefreshTokenDays);
    }

    public int MaxFailedLoginAttempts { get; }
    public TimeSpan LockoutDuration { get; }
    public TimeSpan RefreshTokenLifetime { get; }
}
