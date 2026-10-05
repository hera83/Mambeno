using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace api.Services.Authentication;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration,
    ApiDbContext dbContext,
    IProblemDetailsService problemDetailsService)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var headerValues))
        {
            return AuthenticateResult.Fail($"Missing {ApiKeyAuthenticationOptions.HeaderName} header.");
        }

        var providedKey = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return AuthenticateResult.Fail($"Missing {ApiKeyAuthenticationOptions.HeaderName} header.");
        }

        var masterKey = configuration["ApiKeys:MasterKey"];
        if (!string.IsNullOrEmpty(masterKey) &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(providedKey), Encoding.UTF8.GetBytes(masterKey)))
        {
            return AuthenticateResult.Success(BuildTicket(
            [
                new Claim(ClaimTypes.Name, "master"),
                new Claim(ApiKeyClaimTypes.KeyType, ApiKeyClaimTypes.Master)
            ]));
        }

        var keyHash = ApiKeyGenerator.Hash(providedKey);
        var now = DateTime.UtcNow;
        var apiKey = await dbContext.ApiKeys
            .Where(k => k.KeyHash == keyHash && k.IsActive)
            .Where(k => k.ExpiresAt == null || k.ExpiresAt > now)
            .FirstOrDefaultAsync();

        if (apiKey is null)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        return AuthenticateResult.Success(BuildTicket(
        [
            new Claim(ClaimTypes.Name, apiKey.Name),
            new Claim(ApiKeyClaimTypes.KeyId, apiKey.Id.ToString()),
            new Claim(ApiKeyClaimTypes.KeyType, ApiKeyClaimTypes.Standard)
        ]));
    }

    private AuthenticationTicket BuildTicket(IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationTicket(principal, Scheme.Name);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = Context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Detail = $"A valid {ApiKeyAuthenticationOptions.HeaderName} header is required."
            }
        });
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = Context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Detail = "The provided API key does not have permission to access this resource."
            }
        });
    }
}
