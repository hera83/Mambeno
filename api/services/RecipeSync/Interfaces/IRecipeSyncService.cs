using api.Data.Models;

namespace api.Services.RecipeSync.Interfaces;

public interface IRecipeSyncService
{
    /// <summary>
    /// Synkroniserer kategorier og opskrifter fra Mambeno til databasen: tilføjer nye, opdaterer
    /// ændrede og sletter forsvundne (bag flere sikkerhedskontroller). Fejl kastes ikke, men gemmes på
    /// den returnerede <see cref="RecipeSyncRun"/>; kun annullering kastes videre.
    /// </summary>
    Task<RecipeSyncRun> SyncAsync(CancellationToken cancellationToken = default);
}
