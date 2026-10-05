using Microsoft.AspNetCore.Authentication;

namespace api.Services.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string Scheme = "ApiKey";
    public const string HeaderName = "x-api-key";
}
