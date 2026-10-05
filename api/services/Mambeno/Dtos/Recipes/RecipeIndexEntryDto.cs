namespace api.Services.Mambeno.Dtos.Recipes;

/// <summary>
/// Den lette udgave af en opskrift (kun id, ændringsdato og kategorier), som hentes for alle
/// opskrifter ved hver sync for at finde nye, ændrede og slettede opskrifter.
/// </summary>
public sealed class RecipeIndexEntryDto
{
    public int Id { get; set; }
    public DateTime ModifiedGmt { get; set; }
    public List<int> Categories { get; set; } = [];
}
