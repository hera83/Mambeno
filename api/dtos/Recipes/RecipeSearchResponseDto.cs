namespace api.Dtos.Recipes;

/// <summary>En side af opskriftslisten - samme form som Mors Opskrifter.</summary>
public class RecipeSearchResponseDto
{
    public List<RecipeDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
