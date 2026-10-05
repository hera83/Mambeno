namespace api.Data.Models;

public class RecipeStep
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Mambenos trin-gruppe, fx "Dag 1"/"Dag 2". Tom når fremgangsmåden ikke er grupperet.</summary>
    public string? GroupName { get; set; }

    public string Text { get; set; } = string.Empty;
}
