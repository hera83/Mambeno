namespace api.Services.Mambeno.Dtos.Recipes;

/// <summary>En fuld opskrift fra /wp-json/wp/v2/recipe (kun de felter vi beder om via _fields).</summary>
public sealed class RecipeDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public RenderedTextDto Title { get; set; } = new();
    public DateTime DateGmt { get; set; }
    public DateTime ModifiedGmt { get; set; }
    public List<int> Categories { get; set; } = [];
    public RecipeMetaDto Meta { get; set; } = new();
}
