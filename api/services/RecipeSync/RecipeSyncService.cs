using System.Globalization;
using api.Data;
using api.Data.Models;
using api.Services.Mambeno;
using api.Services.Mambeno.Dtos.Categories;
using api.Services.Mambeno.Dtos.Recipes;
using api.Services.Mambeno.Interfaces;
using api.Services.RecipeSync.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace api.Services.RecipeSync;

/// <summary>
/// Spejler Mambenos kategorier og opskrifter ind i databasen med så få og så små kald som muligt:
/// <list type="number">
/// <item>Henter alle kategorier (1 kald) og et let indeks over alle opskrifter (id + ændringsdato + kategorier, ~50 kald).</item>
/// <item>Henter kun detaljer for nye, ændrede og en lille rullende genopfrisknings-portion (100 pr. kald).</item>
/// <item>Opdaterer kategori-tilknytninger for resten direkte fra indekset (ingen ekstra kald).</item>
/// <item>Sletter forsvundne opskrifter/kategorier - men kun hvis indekset er komplet, antallet er under
/// sikkerhedsgrænsen, og Mambeno for hver enkelt bekræfter, at den er væk.</item>
/// </list>
/// </summary>
public class RecipeSyncService(
    ApiDbContext db,
    IMambenoService mambeno,
    IConfiguration configuration,
    ILogger<RecipeSyncService> logger) : IRecipeSyncService
{
    private const int MaxMessageLength = 4000;

    // Sikrer at to kørsler aldrig overlapper (fx worker + en senere manuel trigger).
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    public async Task<RecipeSyncRun> SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await SyncLock.WaitAsync(0, cancellationToken))
        {
            throw new InvalidOperationException("En opskrifts-sync kører allerede.");
        }

        try
        {
            return await RunAsync(cancellationToken);
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private async Task<RecipeSyncRun> RunAsync(CancellationToken cancellationToken)
    {
        var settings = RecipeSyncSettings.Load(configuration);
        await MarkInterruptedRunsAsync(cancellationToken);

        var run = new RecipeSyncRun();
        db.RecipeSyncRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        var notes = new List<string>();
        logger.LogInformation("Opskrifts-sync fra Mambeno startet (kørsel {RunId})", run.Id);

        try
        {
            // Hent alt fra Mambeno FØR der ændres noget lokalt, så en fejl her aldrig efterlader en halv tilstand.
            var remoteCategories = await mambeno.GetCategoriesAsync(cancellationToken);
            if (remoteCategories.Count == 0)
            {
                throw new InvalidOperationException("Mambeno returnerede ingen kategorier - kørslen er afbrudt, og intet er ændret.");
            }

            var index = await mambeno.GetRecipeIndexAsync(cancellationToken);
            if (index.Entries.Count == 0)
            {
                throw new InvalidOperationException("Mambeno returnerede ingen opskrifter - kørslen er afbrudt, og intet er ændret.");
            }

            logger.LogInformation(
                "Mambeno har {CategoryCount} kategorier og {RecipeCount} opskrifter (oplyst: {ReportedTotal})",
                remoteCategories.Count, index.Entries.Count, index.ReportedTotal);

            var categories = await UpsertCategoriesAsync(remoteCategories, run, cancellationToken);
            var recipes = await UpsertRecipesAsync(index, categories, settings, run, cancellationToken);
            await DeleteMissingRecipesAsync(index, recipes, settings, run, notes, cancellationToken);
            await DeleteMissingCategoriesAsync(remoteCategories, categories, settings, run, notes, cancellationToken);

            run.Status = RecipeSyncStatus.Succeeded;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            run.Status = RecipeSyncStatus.Failed;
            notes.Add("Afbrudt fordi appen lukkede ned.");
            await FinishAsync(run, notes);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Opskrifts-sync fra Mambeno fejlede (kørsel {RunId})", run.Id);
            run.Status = RecipeSyncStatus.Failed;
            notes.Add(ex.Message);
        }

        await FinishAsync(run, notes);

        logger.LogInformation(
            "Opskrifts-sync {RunId} afsluttet med status {Status}: kategorier +{CategoriesAdded}/~{CategoriesUpdated}/-{CategoriesDeleted}, " +
            "opskrifter +{RecipesAdded}/~{RecipesUpdated}/-{RecipesDeleted}, genopfrisket {RecipesRefreshed}, kategori-tilknytninger ændret {LinksUpdated}",
            run.Id, run.Status,
            run.CategoriesAdded, run.CategoriesUpdated, run.CategoriesDeleted,
            run.RecipesAdded, run.RecipesUpdated, run.RecipesDeleted,
            run.RecipesRefreshed, run.RecipeCategoryLinksUpdated);

        return run;
    }

    private async Task<Dictionary<int, Category>> UpsertCategoriesAsync(
        List<CategoryDto> remoteCategories,
        RecipeSyncRun run,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var local = await db.Categories.ToDictionaryAsync(c => c.MambenoId, cancellationToken);
        var added = new HashSet<int>();
        var updated = new HashSet<int>();

        foreach (var remote in remoteCategories)
        {
            var name = HtmlText.ToPlainText(remote.Name);
            if (!local.TryGetValue(remote.Id, out var category))
            {
                category = new Category { MambenoId = remote.Id, Name = name, Slug = remote.Slug, CreatedAt = now, UpdatedAt = now };
                db.Categories.Add(category);
                local[remote.Id] = category;
                added.Add(remote.Id);
            }
            else if (category.Name != name || category.Slug != remote.Slug)
            {
                category.Name = name;
                category.Slug = remote.Slug;
                category.UpdatedAt = now;
                updated.Add(remote.Id);
            }
        }

        // Gem først, så nye kategorier har et Id før forældre-relationerne sættes.
        await db.SaveChangesAsync(cancellationToken);

        foreach (var remote in remoteCategories)
        {
            var category = local[remote.Id];
            var parent = remote.Parent != 0 && local.TryGetValue(remote.Parent, out var p) ? p : null;
            if (category.ParentId != parent?.Id)
            {
                category.Parent = parent;
                category.ParentId = parent?.Id;
                category.UpdatedAt = now;
                if (!added.Contains(remote.Id))
                {
                    updated.Add(remote.Id);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        run.CategoriesAdded = added.Count;
        run.CategoriesUpdated = updated.Count;
        return local;
    }

    private async Task<Dictionary<int, Recipe>> UpsertRecipesAsync(
        RecipeIndexDto index,
        IReadOnlyDictionary<int, Category> categories,
        RecipeSyncSettings settings,
        RecipeSyncRun run,
        CancellationToken cancellationToken)
    {
        var local = await db.Recipes
            .Include(r => r.Categories)
            .ToDictionaryAsync(r => r.MambenoId, cancellationToken);

        var newIds = index.Entries
            .Where(e => !local.ContainsKey(e.Id))
            .Select(e => e.Id)
            .ToList();
        var changedIds = index.Entries
            .Where(e => local.TryGetValue(e.Id, out var r) && r.SourceModifiedAt != AsUtc(e.ModifiedGmt))
            .Select(e => e.Id)
            .ToHashSet();

        // Rullende genopfriskning: de længst siden hentede (uændrede) opskrifter hentes igen, så evt.
        // ændringer Mambeno ikke har stemplet med en ny ændringsdato også bliver fanget med tiden.
        var indexIds = index.Entries.Select(e => e.Id).ToHashSet();
        var refreshIds = settings.RefreshPerRun <= 0
            ? []
            : local.Values
                .Where(r => indexIds.Contains(r.MambenoId) && !changedIds.Contains(r.MambenoId))
                .OrderBy(r => r.LastFetchedAt)
                .ThenBy(r => r.Id)
                .Take(settings.RefreshPerRun)
                .Select(r => r.MambenoId)
                .ToList();

        var toFetch = newIds.Concat(changedIds).Concat(refreshIds).ToList();
        logger.LogInformation(
            "Henter detaljer for {FetchCount} opskrifter: {NewCount} nye, {ChangedCount} ændrede, {RefreshCount} til genopfriskning",
            toFetch.Count, newIds.Count, changedIds.Count, refreshIds.Count);

        var fetchedIds = new HashSet<int>();
        var processed = 0;
        var pageFallbacks = 0;
        var skipped = 0;

        await foreach (var batch in mambeno.GetRecipeBatchesAsync(toFetch, cancellationToken))
        {
            var now = DateTime.UtcNow;

            // Indlæs eksisterende ingredienser/trin for batchen, så de kan erstattes (EF sletter de gamle som orphans).
            var existingRecipeIds = batch
                .Where(d => local.ContainsKey(d.Id))
                .Select(d => local[d.Id].Id)
                .ToList();
            if (existingRecipeIds.Count > 0)
            {
                await db.RecipeIngredients.Where(i => existingRecipeIds.Contains(i.RecipeId)).LoadAsync(cancellationToken);
                await db.RecipeSteps.Where(s => existingRecipeIds.Contains(s.RecipeId)).LoadAsync(cancellationToken);
            }

            foreach (var dto in batch)
            {
                var ingredients = dto.Meta.RecipeIngredients;
                var instructions = dto.Meta.RecipeInstructions;

                if (ingredients is null || instructions is null)
                {
                    // WordPress returnerer null hvis bare én værdi ikke passer til feltets skema; de
                    // fulde data ligger så på selve opskriftssiden. Kun disse opskrifter koster et sidekald.
                    pageFallbacks++;
                    try
                    {
                        var page = await mambeno.GetRecipePageDataAsync(dto.Link, cancellationToken);
                        ingredients ??= page.Ingredients;
                        instructions ??= page.Instructions;
                    }
                    catch (MambenoException ex)
                    {
                        logger.LogWarning(ex, "Kunne ikke hente opskriftssiden for Mambeno-opskrift {MambenoId} ({Url})", dto.Id, dto.Link);
                    }

                    if (ingredients is null || instructions is null)
                    {
                        // Gem hellere intet end en halv opskrift - den bliver forsøgt igen ved næste kørsel.
                        skipped++;
                        logger.LogWarning(
                            "Mambeno-opskrift {MambenoId} ({Url}) mangler {Missing} - springes over og prøves igen ved næste kørsel",
                            dto.Id, dto.Link, ingredients is null ? "ingredienser" : "fremgangsmåde");
                        continue;
                    }
                }

                if (!local.TryGetValue(dto.Id, out var recipe))
                {
                    recipe = new Recipe { MambenoId = dto.Id, CreatedAt = now };
                    db.Recipes.Add(recipe);
                    local[dto.Id] = recipe;
                    run.RecipesAdded++;
                }
                else if (changedIds.Contains(dto.Id))
                {
                    run.RecipesUpdated++;
                }
                else
                {
                    run.RecipesRefreshed++;
                }

                ApplyRecipe(recipe, dto, ingredients, instructions, now);
                SetCategories(recipe, dto.Categories, categories);
                fetchedIds.Add(dto.Id);
            }

            await db.SaveChangesAsync(cancellationToken);

            processed += batch.Count;
            logger.LogInformation("Opskrifts-sync: {Processed}/{Total} opskrifter behandlet", processed, toFetch.Count);
        }

        if (pageFallbacks > 0 || skipped > 0)
        {
            logger.LogInformation(
                "{PageFallbacks} opskrifter blev læst fra selve opskriftssiden; {Skipped} blev sprunget over",
                pageFallbacks, skipped);
        }

        // De opskrifter vi ikke hentede igen kan stadig have fået nye/fjernede kategorier - det kan ses
        // direkte i indekset, så det kræver ingen ekstra kald.
        foreach (var entry in index.Entries)
        {
            if (!fetchedIds.Contains(entry.Id)
                && local.TryGetValue(entry.Id, out var recipe)
                && SetCategories(recipe, entry.Categories, categories))
            {
                run.RecipeCategoryLinksUpdated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return local;
    }

    private async Task DeleteMissingRecipesAsync(
        RecipeIndexDto index,
        Dictionary<int, Recipe> local,
        RecipeSyncSettings settings,
        RecipeSyncRun run,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        var remoteIds = index.Entries.Select(e => e.Id).ToHashSet();
        var candidates = local.Values.Where(r => !remoteIds.Contains(r.MambenoId)).ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        // Kontrol 1: indekset skal være komplet, ellers kan "mangler" bare betyde "ikke nået at hente".
        if (!index.IsComplete)
        {
            SkipDeletions(run, notes,
                $"Sletning af {candidates.Count} opskrifter sprunget over: opskriftslisten fra Mambeno var ufuldstændig " +
                $"({index.Entries.Count} af {index.ReportedTotal}). Prøves igen ved næste kørsel.");
            return;
        }

        // Kontrol 2: aldrig masse-sletning - langt flere forsvundne end normalt tyder på en fejl, ikke en ændring.
        var allowed = settings.GetAllowedDeletions(local.Count);
        if (candidates.Count > allowed)
        {
            SkipDeletions(run, notes,
                $"Sletning sprunget over: {candidates.Count} af {local.Count} opskrifter mangler hos Mambeno, men højst {allowed} må slettes pr. kørsel. " +
                "Tjek Mambeno manuelt; er sletningen korrekt, så hæv RecipeSync:MaxDeletionPercent midlertidigt.",
                isError: true);
            return;
        }

        // Kontrol 3: Mambeno skal for hver enkelt opskrift entydigt svare "findes ikke".
        foreach (var recipe in candidates)
        {
            if (await mambeno.RecipeExistsAsync(recipe.MambenoId, cancellationToken))
            {
                logger.LogWarning("Mambeno-opskrift {MambenoId} manglede i listen, men findes stadig - beholdes", recipe.MambenoId);
                continue;
            }

            logger.LogInformation("Sletter opskrift {MambenoId} '{Title}', som ikke længere findes hos Mambeno", recipe.MambenoId, recipe.Title);
            db.Recipes.Remove(recipe);
            local.Remove(recipe.MambenoId);
            run.RecipesDeleted++;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task DeleteMissingCategoriesAsync(
        List<CategoryDto> remoteCategories,
        Dictionary<int, Category> local,
        RecipeSyncSettings settings,
        RecipeSyncRun run,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        var remoteIds = remoteCategories.Select(c => c.Id).ToHashSet();
        var candidates = local.Values.Where(c => !remoteIds.Contains(c.MambenoId)).ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var allowed = settings.GetAllowedDeletions(local.Count);
        if (candidates.Count > allowed)
        {
            SkipDeletions(run, notes,
                $"Sletning sprunget over: {candidates.Count} af {local.Count} kategorier mangler hos Mambeno, men højst {allowed} må slettes pr. kørsel.",
                isError: true);
            return;
        }

        foreach (var category in candidates)
        {
            if (await mambeno.CategoryExistsAsync(category.MambenoId, cancellationToken))
            {
                logger.LogWarning("Mambeno-kategori {MambenoId} manglede i listen, men findes stadig - beholdes", category.MambenoId);
                continue;
            }

            logger.LogInformation("Sletter kategori {MambenoId} '{Name}', som ikke længere findes hos Mambeno", category.MambenoId, category.Name);
            foreach (var child in category.Children.ToList())
            {
                child.Parent = null;
                child.ParentId = null;
            }

            db.Categories.Remove(category);
            local.Remove(category.MambenoId);
            run.CategoriesDeleted++;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private void SkipDeletions(RecipeSyncRun run, List<string> notes, string message, bool isError = false)
    {
        run.DeletionsSkipped = true;
        notes.Add(message);
        if (isError)
        {
            logger.LogError("{Message}", message);
        }
        else
        {
            logger.LogWarning("{Message}", message);
        }
    }

    private static void ApplyRecipe(
        Recipe recipe,
        RecipeDto dto,
        List<RecipeIngredientDto> ingredients,
        List<RecipeInstructionDto> instructions,
        DateTime now)
    {
        var meta = dto.Meta;

        recipe.Title = HtmlText.ToPlainText(dto.Title.Rendered);
        recipe.Slug = dto.Slug;
        recipe.SourceUrl = dto.Link;
        recipe.Description = HtmlText.ToPlainTextOrNull(meta.RecipeDescription);
        recipe.KitchenTimeMinutes = ToWholeNumber(meta.RecipePrepTime);
        recipe.WaitingTimeMinutes = ToWholeNumber(meta.RecipeCookTime);
        recipe.Servings = ToPositiveWholeNumber(meta.RecipeServingsNormalized) ?? ToPositiveWholeNumber(meta.RecipeServings);
        recipe.ServingsDescription = HtmlText.ToPlainTextOrNull(meta.RecipeServingsType);
        recipe.Notes = HtmlText.ToPlainTextOrNull(meta.RecipeNotes);
        recipe.PublishedAt = AsUtc(dto.DateGmt);
        recipe.SourceModifiedAt = AsUtc(dto.ModifiedGmt);
        recipe.LastFetchedAt = now;

        recipe.Ingredients.Clear();
        foreach (var ingredient in ingredients)
        {
            var name = HtmlText.ToPlainText(ingredient.Ingredient);
            var amount = ingredient.Amount.Trim();
            if (name.Length == 0 && amount.Length == 0)
            {
                continue;
            }

            recipe.Ingredients.Add(new RecipeIngredient
            {
                SortOrder = recipe.Ingredients.Count,
                GroupName = HtmlText.ToPlainTextOrNull(ingredient.Group),
                Amount = amount,
                Quantity = amount.Length == 0 ? null : ingredient.AmountNormalized ?? ParseQuantity(amount),
                Unit = ingredient.Unit.Trim(),
                Name = name,
                Notes = HtmlText.ToPlainTextOrNull(ingredient.Notes),
                MambenoIngredientId = ingredient.IngredientId
            });
        }

        recipe.Steps.Clear();
        foreach (var instruction in instructions)
        {
            var text = HtmlText.ToPlainText(instruction.Description);
            if (text.Length == 0)
            {
                continue;
            }

            recipe.Steps.Add(new RecipeStep
            {
                SortOrder = recipe.Steps.Count,
                GroupName = HtmlText.ToPlainTextOrNull(instruction.Group),
                Text = text
            });
        }
    }

    /// <summary>Sætter opskriftens kategorier til præcis de givne (ukendte id'er ignoreres). Returnerer true hvis noget blev ændret.</summary>
    private static bool SetCategories(Recipe recipe, IEnumerable<int> mambenoCategoryIds, IReadOnlyDictionary<int, Category> categories)
    {
        var wanted = mambenoCategoryIds.Where(categories.ContainsKey).ToHashSet();

        var toRemove = recipe.Categories.Where(c => !wanted.Contains(c.MambenoId)).ToList();
        foreach (var category in toRemove)
        {
            recipe.Categories.Remove(category);
        }

        var current = recipe.Categories.Select(c => c.MambenoId).ToHashSet();
        var toAdd = wanted.Where(id => !current.Contains(id)).ToList();
        foreach (var id in toAdd)
        {
            recipe.Categories.Add(categories[id]);
        }

        return toRemove.Count > 0 || toAdd.Count > 0;
    }

    private async Task MarkInterruptedRunsAsync(CancellationToken cancellationToken)
    {
        await db.RecipeSyncRuns
            .Where(r => r.Status == RecipeSyncStatus.Running)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, RecipeSyncStatus.Failed)
                .SetProperty(r => r.FinishedAt, DateTime.UtcNow)
                .SetProperty(r => r.Message, "Afbrudt - appen blev stoppet under kørslen."), cancellationToken);
    }

    private async Task FinishAsync(RecipeSyncRun run, List<string> notes)
    {
        run.FinishedAt = DateTime.UtcNow;
        var message = string.Join(Environment.NewLine, notes);
        run.Message = message.Length == 0 ? null : message[..Math.Min(message.Length, MaxMessageLength)];

        try
        {
            // Smid evt. halvfærdige ændringer fra en fejlet batch væk, så kun selve kørsels-rækken gemmes.
            db.ChangeTracker.Clear();
            db.RecipeSyncRuns.Update(run);
            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kunne ikke gemme status for opskrifts-sync {RunId}", run.Id);
        }
    }

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static int? ToWholeNumber(double? value) => value is null ? null : (int)Math.Round(value.Value);

    private static int? ToPositiveWholeNumber(double? value) => value is > 0 ? (int)Math.Round(value.Value) : null;

    private static double? ParseQuantity(string amount)
        => double.TryParse(amount.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;
}
