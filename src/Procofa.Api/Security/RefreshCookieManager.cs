using Procofa.Api.Configuration;

namespace Procofa.Api.Security;

public sealed class RefreshCookieManager(
    RefreshCookieSettings settings)
{
    public string? Read(HttpRequest request)
    {
        return request.Cookies.TryGetValue(
            settings.Name,
            out var token)
            ? token
            : null;
    }

    public void Write(
        HttpResponse response,
        string rawRefreshToken,
        DateTime expiresAtUtc)
    {
        response.Cookies.Append(
            settings.Name,
            rawRefreshToken,
            CreateOptions(
                new DateTimeOffset(expiresAtUtc)));
    }

    public void Delete(HttpResponse response)
    {
        response.Cookies.Delete(
            settings.Name,
            CreateOptions(expires: null));
    }

    private CookieOptions CreateOptions(
        DateTimeOffset? expires)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = settings.Secure,
            SameSite = settings.SameSite,
            Path = settings.Path,
            Expires = expires,
            IsEssential = true,
        };
    }
}
