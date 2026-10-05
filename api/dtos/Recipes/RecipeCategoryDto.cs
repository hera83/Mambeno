namespace api.Dtos.Recipes;

/// <summary>En af de kategorier en opskrift ligger i.</summary>
public class RecipeCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
