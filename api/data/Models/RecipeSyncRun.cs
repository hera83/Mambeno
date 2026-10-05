namespace api.Data.Models;

/// <summary>
/// Én kørsel af opskrifts-sync'en mod Mambeno. Bruges både som historik og af workeren til at
/// regne ud, hvornår næste kørsel skal ske.
/// </summary>
public class RecipeSyncRun
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public RecipeSyncStatus Status { get; set; } = RecipeSyncStatus.Running;

    public int CategoriesAdded { get; set; }
    public int CategoriesUpdated { get; set; }
    public int CategoriesDeleted { get; set; }

    public int RecipesAdded { get; set; }
    public int RecipesUpdated { get; set; }
    public int RecipesDeleted { get; set; }

    /// <summary>Uændrede opskrifter der blev hentet igen som led i den rullende genopfriskning.</summary>
    public int RecipesRefreshed { get; set; }

    /// <summary>Opskrifter hvor kun kategori-tilknytningen ændrede sig.</summary>
    public int RecipeCategoryLinksUpdated { get; set; }

    /// <summary>True hvis sletning blev sprunget over af en sikkerhedskontrol (se <see cref="Message"/>).</summary>
    public bool DeletionsSkipped { get; set; }

    public string? Message { get; set; }
}

public enum RecipeSyncStatus
{
    Running,
    Succeeded,
    Failed
}
