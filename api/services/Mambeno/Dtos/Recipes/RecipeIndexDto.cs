namespace api.Services.Mambeno.Dtos.Recipes;

public sealed class RecipeIndexDto
{
    public List<RecipeIndexEntryDto> Entries { get; set; } = [];

    /// <summary>Antallet af opskrifter Mambeno selv oplyste (X-WP-Total) på første side.</summary>
    public int ReportedTotal { get; set; }

    /// <summary>
    /// True hvis vi har mindst lige så mange unikke opskrifter som Mambeno oplyste. Er den false
    /// (fx fordi en opskrift blev slettet midt i pagineringen), må indekset ikke bruges til sletning.
    /// </summary>
    public bool IsComplete => Entries.Count > 0 && Entries.Count >= ReportedTotal;
}
