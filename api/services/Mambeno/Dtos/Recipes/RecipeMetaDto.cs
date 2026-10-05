using System.Text.Json.Serialization;

namespace api.Services.Mambeno.Dtos.Recipes;

/// <summary>
/// WP Ultimate Recipe-felterne fra opskriftens "meta". Bemærk at WordPress returnerer null for
/// recipe_ingredients/recipe_instructions, hvis bare én værdi ikke passer til feltets skema (fx et
/// tal i stedet for tekst) - så skal data hentes fra selve opskriftssiden i stedet.
/// </summary>
public sealed class RecipeMetaDto
{
    public string? RecipeDescription { get; set; }

    /// <summary>"Køkkentid" i minutter.</summary>
    public double? RecipePrepTime { get; set; }

    /// <summary>"Ventetid" i minutter.</summary>
    public double? RecipeCookTime { get; set; }

    public double? RecipeServings { get; set; }
    public double? RecipeServingsNormalized { get; set; }
    public string? RecipeServingsType { get; set; }
    public string? RecipeNotes { get; set; }

    [JsonConverter(typeof(LenientListJsonConverter<RecipeIngredientDto>))]
    public List<RecipeIngredientDto>? RecipeIngredients { get; set; }

    [JsonConverter(typeof(LenientListJsonConverter<RecipeInstructionDto>))]
    public List<RecipeInstructionDto>? RecipeInstructions { get; set; }
}
