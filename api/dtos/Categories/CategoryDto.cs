namespace api.Dtos.Categories;

/// <summary>En kategori og hvor mange opskrifter der ligger i den. Samme form som Mors Opskrifters CategoryDto.</summary>
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Ikonnavn (Material Symbols). "cookie" hvis der ikke er sat et.</summary>
    public string Icon { get; set; } = string.Empty;

    public int RecipeCount { get; set; }

    /// <summary>Ekstra felt: id på overkategorien (fx "Aftensmad" for "Gæstemad"), null for en topkategori.</summary>
    public int? ParentId { get; set; }
}
