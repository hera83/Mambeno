namespace api.Dtos.Recipes;

/// <summary>En ingredienslinje, fx "2 dl mælk". Samme form som Mors Opskrifters IngredientDto.</summary>
public class IngredientDto
{
    /// <summary>Mængden præcis som den står hos Mambeno, fx "0.5". Tom hvis der ikke er en mængde.</summary>
    public string Amount { get; set; } = string.Empty;

    /// <summary>Mængden som tal, eller null hvis der ikke er en mængde.</summary>
    public double? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    /// <summary>Ingrediensens navn inkl. Mambenos note, fx "grillost - også kaldet halloumi".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Ekstra felt: Mambenos ingrediensgruppe, fx "Dressing" eller "Dej". Null når opskriften ikke er grupperet.</summary>
    public string? Group { get; set; }

    /// <summary>Ekstra felt: id på en anden opskrift i dette API, hvis ingrediensen selv er en opskrift (fx burgerboller).</summary>
    public int? LinkedRecipeId { get; set; }
}
