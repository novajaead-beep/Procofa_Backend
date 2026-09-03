using Microsoft.AspNetCore.Http;

namespace Procofa.Api.Configuration;

public sealed record RefreshCookieSettings(
    string Name,
    bool Secure,
    SameSiteMode SameSite,
    string Path);
