using Microsoft.AspNetCore.Http;

namespace Procofa.Api.Configuration;

internal static class RefreshCookieSettingsFactory
{
    public static RefreshCookieSettings Create(
        IConfiguration configuration)
    {
        var name =
            configuration[
                "Auth:RefreshCookie:Name"];

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "Auth:RefreshCookie:Name no está configurado.");
        }

        var path =
            configuration[
                "Auth:RefreshCookie:Path"];

        if (string.IsNullOrWhiteSpace(path) ||
            !path.StartsWith('/'))
        {
            throw new InvalidOperationException(
                "Auth:RefreshCookie:Path debe comenzar con '/'.");
        }

        var secure =
            configuration.GetValue(
                "Auth:RefreshCookie:Secure",
                true);

        var sameSiteRaw =
            configuration[
                "Auth:RefreshCookie:SameSite"];

        if (!Enum.TryParse<SameSiteMode>(
                sameSiteRaw,
                ignoreCase: true,
                out var sameSite))
        {
            throw new InvalidOperationException(
                "Auth:RefreshCookie:SameSite debe ser Strict, Lax o None.");
        }

        if (sameSite == SameSiteMode.None &&
            !secure)
        {
            throw new InvalidOperationException(
                "SameSite=None requiere Secure=true.");
        }

        return new RefreshCookieSettings(
            name,
            secure,
            sameSite,
            path);
    }
}
