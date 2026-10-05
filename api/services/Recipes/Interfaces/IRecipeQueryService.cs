using api.Dtos.Categories;
using api.Dtos.Recipes;

namespace api.Services.Recipes.Interfaces;

/// <summary>Læser de spejlede Mambeno-kategorier og -opskrifter i samme form som Mors Opskrifter-API'et.</summary>
public interface IRecipeQueryService
{
    Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetCategoryAsync(int id, CancellationToken cancellationToken = default);
    Task<RecipeSearchResponseDto> SearchRecipesAsync(RecipeSearchRequestDto request, CancellationToken cancellationToken = default);
    Task<RecipeDto?> GetRecipeAsync(int id, CancellationToken cancellationToken = default);
}
