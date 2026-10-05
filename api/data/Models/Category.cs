namespace api.Data.Models;

/// <summary>
/// En opskriftskategori spejlet fra Mambeno (en WordPress-kategori, fx "Aftensmad" eller "Gæstemad").
/// </summary>
public class Category
{
    public int Id { get; set; }

    /// <summary>Kategoriens id hos Mambeno (WordPress term id) - bruges til at matche ved sync.</summary>
    public int MambenoId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    /// <summary>Ikon-navn (fx "cookie"). Sættes aldrig af sync'en, så det kan vedligeholdes manuelt.</summary>
    public string? Icon { get; set; }

    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = [];

    public List<Recipe> Recipes { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
