namespace api.Data.Models;

/// <summary>
/// En offline kopi af en opskrift fra Mambeno. Billeder gemmes bevidst ikke.
/// </summary>
public class Recipe
{
    public int Id { get; set; }

    /// <summary>Opskriftens id hos Mambeno (WordPress post id) - bruges til at matche ved sync.</summary>
    public int MambenoId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>"Køkkentid" på Mambeno (WPURP-feltet recipe_prep_time).</summary>
    public int? KitchenTimeMinutes { get; set; }

    /// <summary>"Ventetid" på Mambeno (WPURP-feltet recipe_cook_time).</summary>
    public int? WaitingTimeMinutes { get; set; }

    public int? Servings { get; set; }

    /// <summary>Mambenos portionstekst, fx "2 voksne og 2 børn".</summary>
    public string? ServingsDescription { get; set; }

    /// <summary>Mambenos "Tip"-sektion.</summary>
    public string? Notes { get; set; }

    public DateTime PublishedAt { get; set; }

    /// <summary>Hvornår opskriften sidst blev ændret hos Mambeno (UTC) - bruges til at finde ændrede opskrifter.</summary>
    public DateTime SourceModifiedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Hvornår detaljerne sidst blev hentet fra Mambeno (UTC) - bruges til den rullende genopfriskning.</summary>
    public DateTime LastFetchedAt { get; set; } = DateTime.UtcNow;

    public List<RecipeIngredient> Ingredients { get; set; } = [];
    public List<RecipeStep> Steps { get; set; } = [];
    public List<Category> Categories { get; set; } = [];
}
