using System.Text.Json.Serialization;

namespace api.Services.Mambeno.Dtos.Recipes;

/// <summary>
/// Én ingrediens. Formatet er det samme i REST API'ets meta og i sidens window.recipeIngredients,
/// men på siden kan fx "amount" være et tal i stedet for tekst - derfor de tolerante converters.
/// </summary>
public sealed class RecipeIngredientDto
{
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string Amount { get; set; } = string.Empty;

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string Unit { get; set; } = string.Empty;

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string Ingredient { get; set; } = string.Empty;

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string Notes { get; set; } = string.Empty;

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string Group { get; set; } = string.Empty;

    public int? IngredientId { get; set; }
    public double? AmountNormalized { get; set; }
}
