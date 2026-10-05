namespace api.Services.Mambeno.Dtos.Recipes;

/// <summary>
/// Ingredienser og fremgangsmåde læst direkte fra opskriftens HTML-side. Bruges kun som fallback,
/// når REST API'et returnerer null for et af felterne. Et felt er null, hvis det ikke kunne findes.
/// </summary>
public sealed class RecipePageDataDto
{
    public List<RecipeIngredientDto>? Ingredients { get; set; }
    public List<RecipeInstructionDto>? Instructions { get; set; }
}
