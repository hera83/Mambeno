namespace api.Data.Models;

public class RecipeIngredient
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Mambenos ingrediensgruppe, fx "Dressing" eller "Dag 1". Tom når opskriften ikke er grupperet.</summary>
    public string? GroupName { get; set; }

    /// <summary>Mængden som tekst præcis som hos Mambeno, fx "0.5". Tom for fx "salt".</summary>
    public string Amount { get; set; } = string.Empty;

    /// <summary>Mængden som tal (Mambenos amount_normalized). Null når der ikke er angivet en mængde.</summary>
    public double? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }

    /// <summary>Mambenos interne ingrediens-id, så samme ingrediens kan genkendes på tværs af opskrifter.</summary>
    public int? MambenoIngredientId { get; set; }
}
