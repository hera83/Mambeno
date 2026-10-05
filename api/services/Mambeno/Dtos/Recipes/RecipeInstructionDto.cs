using System.Text.Json.Serialization;

namespace api.Services.Mambeno.Dtos.Recipes;

/// <summary>Ét trin i fremgangsmåden. Mambenos "image"-felt ignoreres bevidst (ingen billeder).</summary>
public sealed class RecipeInstructionDto
{
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string Description { get; set; } = string.Empty;

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string Group { get; set; } = string.Empty;
}
