namespace api.Services.Mambeno.Dtos.Recipes;

/// <summary>WordPress' { "rendered": "..." }-felter (HTML-encodet).</summary>
public sealed class RenderedTextDto
{
    public string Rendered { get; set; } = string.Empty;
}
