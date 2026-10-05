using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace api.Services.Authentication;

public class ApiKeySecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeId = "ApiKey";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = ApiKeyAuthenticationOptions.HeaderName,
            Description = "API key required for every request. Use the master key from configuration, or a standard key issued via POST /keys."
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeId, document)] = []
        });

        return Task.CompletedTask;
    }
}
