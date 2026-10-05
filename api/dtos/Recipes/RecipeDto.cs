namespace api.Dtos.Recipes;

/// <summary>
/// En opskrift med ingredienser og fremgangsmåde. Felterne til og med <see cref="Steps"/> har samme navn,
/// type og rækkefølge som Mors Opskrifters RecipeDto; felterne derefter er ekstra Mambeno-data.
/// </summary>
public class RecipeDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>Den mest specifikke af opskriftens kategorier (den med færrest opskrifter). Se <see cref="Categories"/> for dem alle.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Kategoriens navn (tom streng hvis opskriften ikke har en kategori).</summary>
    public string Category { get; set; } = string.Empty;

    public string CategoryIcon { get; set; } = string.Empty;

    /// <summary>Mambenos "Køkkentid" i minutter.</summary>
    public int PrepTimeMinutes { get; set; }

    /// <summary>Mambenos "Ventetid" i minutter.</summary>
    public int CookTimeMinutes { get; set; }

    /// <summary>Køkkentid + ventetid i minutter.</summary>
    public int TotalTimeMinutes { get; set; }

    public int Servings { get; set; }

    /// <summary>Altid null - Mambeno angiver ikke sværhedsgrad.</summary>
    public Difficulty? Difficulty { get; set; }

    public string Author { get; set; } = string.Empty;

    /// <summary>Mambenos "Tip" til opskriften.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>Hvornår opskriften sidst blev ændret hos Mambeno (UTC).</summary>
    public DateTime LastModified { get; set; }

    /// <summary>Altid null - billeder kopieres ikke fra Mambeno.</summary>
    public string? ImageUrl { get; set; }

    public List<IngredientDto> Ingredients { get; set; } = [];

    /// <summary>Fremgangsmådens trin i rækkefølge. Er trinene grupperet (fx "Dag 1"/"Dag 2"), står gruppen foran det første trin i gruppen.</summary>
    public List<string> Steps { get; set; } = [];

    /// <summary>Ekstra felt: Mambenos beskrivelse af retten.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Ekstra felt: alle de kategorier opskriften ligger i.</summary>
    public List<RecipeCategoryDto> Categories { get; set; } = [];

    /// <summary>Ekstra felt: link til opskriften på mambeno.dk.</summary>
    public string SourceUrl { get; set; } = string.Empty;
}
