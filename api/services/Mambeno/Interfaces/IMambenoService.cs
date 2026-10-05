using api.Services.Mambeno.Dtos.Categories;
using api.Services.Mambeno.Dtos.Recipes;

namespace api.Services.Mambeno.Interfaces;

/// <summary>
/// Læseadgang til Mambenos offentlige WordPress REST API (/wp-json/wp/v2). Alle kald går gennem én
/// fælles kø med en fast pause imellem, uanset hvor mange scopes der bruger servicen.
/// </summary>
public interface IMambenoService
{
    Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<bool> CategoryExistsAsync(int mambenoId, CancellationToken cancellationToken = default);

    /// <summary>Henter id, ændringsdato og kategorier for samtlige opskrifter (ca. 1,5 KB pr. 100 opskrifter).</summary>
    Task<RecipeIndexDto> GetRecipeIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter fulde opskrifter for de givne id'er, ét kald pr. PageSize id'er, og afleverer dem batch for
    /// batch så kalderen kan gemme undervejs. Opskrifter der ikke kan læses (eller er forsvundet) udelades.
    /// </summary>
    IAsyncEnumerable<IReadOnlyList<RecipeDto>> GetRecipeBatchesAsync(IReadOnlyCollection<int> mambenoIds, CancellationToken cancellationToken = default);

    /// <summary>True hvis opskriften stadig er offentlig hos Mambeno; false hvis den er slettet/skjult (404/401/403).</summary>
    Task<bool> RecipeExistsAsync(int mambenoId, CancellationToken cancellationToken = default);

    /// <summary>Fallback: læser ingredienser og fremgangsmåde direkte fra opskriftens HTML-side.</summary>
    Task<RecipePageDataDto> GetRecipePageDataAsync(string recipeUrl, CancellationToken cancellationToken = default);
}
