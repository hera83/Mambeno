namespace api.Services.Mambeno.Dtos.Categories;

/// <summary>En WordPress-kategori fra /wp-json/wp/v2/categories.</summary>
public sealed class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    /// <summary>Forældrekategoriens Mambeno-id, 0 for en topkategori.</summary>
    public int Parent { get; set; }

    public int Count { get; set; }
}
