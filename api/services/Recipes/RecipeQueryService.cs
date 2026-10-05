using System.Text.RegularExpressions;
using api.Data;
using api.Data.Models;
using api.Dtos.Categories;
using api.Dtos.Recipes;
using api.Services.Recipes.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace api.Services.Recipes;

public partial class RecipeQueryService(ApiDbContext db) : IRecipeQueryService
{
    private const string DefaultIcon = "cookie";
    private const string Author = "Mambeno";
    private const int MaxPageSize = 200;

    public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await db.Categories
            .AsNoTracking()
            .OrderBy(c => EF.Functions.Collate(c.Name, "NOCASE"))
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Icon = c.Icon ?? DefaultIcon,
                RecipeCount = c.Recipes.Count,
                ParentId = c.ParentId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto?> GetCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Icon = c.Icon ?? DefaultIcon,
                RecipeCount = c.Recipes.Count,
                ParentId = c.ParentId
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RecipeSearchResponseDto> SearchRecipesAsync(RecipeSearchRequestDto request, CancellationToken cancellationToken = default)
    {
        // Samme opførsel som Mors Opskrifter: ugyldige sidetal rettes i stedet for at give fejl.
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = ApplyFilters(db.Recipes.AsNoTracking(), request);
        var totalCount = await query.CountAsync(cancellationToken);

        var pageIds = await ApplySort(query, request.Sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var recipes = await LoadRecipesAsync(pageIds, cancellationToken);
        var items = await MapRecipesAsync(recipes, cancellationToken);

        return new RecipeSearchResponseDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<RecipeDto?> GetRecipeAsync(int id, CancellationToken cancellationToken = default)
    {
        var recipes = await LoadRecipesAsync([id], cancellationToken);
        if (recipes.Count == 0)
        {
            return null;
        }

        return (await MapRecipesAsync(recipes, cancellationToken))[0];
    }

    private static IQueryable<Recipe> ApplyFilters(IQueryable<Recipe> query, RecipeSearchRequestDto request)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // SQLite's LIKE er uden forskel på store/små bogstaver for a-z - præcis som i Mors Opskrifter.
            var pattern = ToLikePattern(request.Search);
            query = query.Where(r =>
                EF.Functions.Like(r.Title, pattern, LikeEscape)
                || EF.Functions.Like(r.Description, pattern, LikeEscape)
                || EF.Functions.Like(r.Notes, pattern, LikeEscape)
                || r.Categories.Any(c => EF.Functions.Like(c.Name, pattern, LikeEscape))
                || r.Ingredients.Any(i => EF.Functions.Like(i.Name, pattern, LikeEscape)));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim();
            query = query.Where(r => r.Categories.Any(c => EF.Functions.Collate(c.Name, "NOCASE") == category));
        }

        if (request.CategoryId is { } categoryId)
        {
            // Matcher alle opskriftens kategorier, ikke kun den viste "primære" - så recipeCount og filteret altid stemmer.
            query = query.Where(r => r.Categories.Any(c => c.Id == categoryId));
        }

        if (request.Difficulty is not null)
        {
            // Mambeno har ingen sværhedsgrader, så ingen opskrift kan matche.
            query = query.Where(_ => false);
        }

        if (request.MaxTotalMinutes is { } maxTotalMinutes)
        {
            // 0 minutter betyder at Mambeno ikke har angivet en tid - de skal ikke ligne lynhurtige retter.
            query = query.Where(r =>
                (r.KitchenTimeMinutes ?? 0) + (r.WaitingTimeMinutes ?? 0) > 0
                && (r.KitchenTimeMinutes ?? 0) + (r.WaitingTimeMinutes ?? 0) <= maxTotalMinutes);
        }

        foreach (var ingredient in request.Ingredient.Where(i => !string.IsNullOrWhiteSpace(i)))
        {
            var pattern = ToLikePattern(ingredient);
            query = query.Where(r => r.Ingredients.Any(i => EF.Functions.Like(i.Name, pattern, LikeEscape)));
        }

        if (request.ModifiedSince is { } modifiedSince)
        {
            var modifiedSinceUtc = modifiedSince.Kind == DateTimeKind.Local
                ? modifiedSince.ToUniversalTime()
                : DateTime.SpecifyKind(modifiedSince, DateTimeKind.Utc);
            query = query.Where(r => r.SourceModifiedAt >= modifiedSinceUtc);
        }

        return query;
    }

    private static IQueryable<Recipe> ApplySort(IQueryable<Recipe> query, RecipeSort sort) => sort switch
    {
        RecipeSort.LastModified => query
            .OrderByDescending(r => r.SourceModifiedAt)
            .ThenBy(r => r.Id),
        RecipeSort.TotalTime => query
            .OrderBy(r => (r.KitchenTimeMinutes ?? 0) + (r.WaitingTimeMinutes ?? 0) == 0) // ukendt tid sidst
            .ThenBy(r => (r.KitchenTimeMinutes ?? 0) + (r.WaitingTimeMinutes ?? 0))
            .ThenBy(r => EF.Functions.Collate(r.Title, "NOCASE"))
            .ThenBy(r => r.Id),
        _ => query
            .OrderBy(r => EF.Functions.Collate(r.Title, "NOCASE"))
            .ThenBy(r => r.Id)
    };

    private async Task<List<Recipe>> LoadRecipesAsync(List<int> ids, CancellationToken cancellationToken)
    {
        var recipes = await db.Recipes
            .AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .Include(r => r.Ingredients.OrderBy(i => i.SortOrder))
            .Include(r => r.Steps.OrderBy(s => s.SortOrder))
            .Include(r => r.Categories)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        // Bevar rækkefølgen fra sorteringen.
        var position = ids.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        return recipes.OrderBy(r => position[r.Id]).ToList();
    }

    private async Task<List<RecipeDto>> MapRecipesAsync(List<Recipe> recipes, CancellationToken cancellationToken)
    {
        if (recipes.Count == 0)
        {
            return [];
        }

        // Antal opskrifter pr. kategori bruges til at vælge den mest specifikke kategori som "primær".
        var categoryRecipeCounts = await db.Categories
            .AsNoTracking()
            .Select(c => new { c.Id, Count = c.Recipes.Count })
            .ToDictionaryAsync(c => c.Id, c => c.Count, cancellationToken);

        // Ingredienser der selv er Mambeno-opskrifter, oversat fra Mambeno-id til vores id.
        var linkedMambenoIds = recipes
            .SelectMany(r => r.Ingredients)
            .Select(i => ParseLinkedRecipe(i.Notes))
            .OfType<int>()
            .Distinct()
            .ToList();
        var localIdsByMambenoId = linkedMambenoIds.Count == 0
            ? []
            : await db.Recipes
                .AsNoTracking()
                .Where(r => linkedMambenoIds.Contains(r.MambenoId))
                .ToDictionaryAsync(r => r.MambenoId, r => r.Id, cancellationToken);

        return recipes
            .Select(r => ToDto(r, categoryRecipeCounts, localIdsByMambenoId))
            .ToList();
    }

    private static RecipeDto ToDto(
        Recipe recipe,
        IReadOnlyDictionary<int, int> categoryRecipeCounts,
        IReadOnlyDictionary<int, int> localIdsByMambenoId)
    {
        var primaryCategory = recipe.Categories
            .OrderBy(c => categoryRecipeCounts.GetValueOrDefault(c.Id, int.MaxValue))
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        var prep = recipe.KitchenTimeMinutes ?? 0;
        var cook = recipe.WaitingTimeMinutes ?? 0;

        return new RecipeDto
        {
            Id = recipe.Id,
            Title = recipe.Title,
            CategoryId = primaryCategory?.Id,
            Category = primaryCategory?.Name ?? string.Empty,
            CategoryIcon = primaryCategory?.Icon ?? DefaultIcon,
            PrepTimeMinutes = prep,
            CookTimeMinutes = cook,
            TotalTimeMinutes = prep + cook,
            Servings = recipe.Servings ?? 0,
            Difficulty = null,
            Author = Author,
            Notes = recipe.Notes ?? string.Empty,
            LastModified = DateTime.SpecifyKind(recipe.SourceModifiedAt, DateTimeKind.Utc),
            ImageUrl = null,
            Ingredients = recipe.Ingredients.Select(i => ToIngredientDto(i, localIdsByMambenoId)).ToList(),
            Steps = ToSteps(recipe.Steps),
            Description = recipe.Description ?? string.Empty,
            Categories = recipe.Categories
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(c => new RecipeCategoryDto { Id = c.Id, Name = c.Name, Icon = c.Icon ?? DefaultIcon })
                .ToList(),
            SourceUrl = recipe.SourceUrl
        };
    }

    private static IngredientDto ToIngredientDto(RecipeIngredient ingredient, IReadOnlyDictionary<int, int> localIdsByMambenoId)
    {
        // Noten er enten en henvisning til en anden opskrift ([mambeno-recipe-link id="89"]) eller en
        // tekst som "- også kaldet halloumi", der vises efter navnet ligesom på mambeno.dk.
        var linkedMambenoId = ParseLinkedRecipe(ingredient.Notes);
        var note = RecipeLinkRegex().Replace(ingredient.Notes ?? string.Empty, string.Empty).Trim();

        var name = ingredient.Name;
        if (note.Length > 0)
        {
            name = note.StartsWith('-') || note.StartsWith('(') ? $"{name} {note}" : $"{name}, {note}";
        }

        return new IngredientDto
        {
            Amount = ingredient.Amount,
            Quantity = ingredient.Quantity,
            Unit = ingredient.Unit,
            Name = name,
            Group = ingredient.GroupName,
            LinkedRecipeId = linkedMambenoId is { } mambenoId && localIdsByMambenoId.TryGetValue(mambenoId, out var localId)
                ? localId
                : null
        };
    }

    /// <summary>Trin er ren tekst; en gruppe (fx "Dag 2") skrives foran det første trin i gruppen.</summary>
    private static List<string> ToSteps(IEnumerable<RecipeStep> steps)
    {
        var result = new List<string>();
        string? currentGroup = null;
        foreach (var step in steps)
        {
            if (step.GroupName is not null && step.GroupName != currentGroup)
            {
                result.Add($"{step.GroupName}: {step.Text}");
            }
            else
            {
                result.Add(step.Text);
            }

            currentGroup = step.GroupName;
        }

        return result;
    }

    private static int? ParseLinkedRecipe(string? notes)
    {
        if (string.IsNullOrEmpty(notes))
        {
            return null;
        }

        var match = RecipeLinkRegex().Match(notes);
        return match.Success && int.TryParse(match.Groups[1].Value, out var id) ? id : null;
    }

    private const string LikeEscape = "\\";

    private static string ToLikePattern(string text)
    {
        var escaped = text.Trim()
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
        return $"%{escaped}%";
    }

    // Mambenos shortcode findes i flere skæve varianter: [mambeno-recipe-link id="89"], id=“89”], id="89”] osv.
    [GeneratedRegex(@"\[?\s*mambeno-recipe-link\s+id\s*=\s*[""“”']?(\d+)[""“”']?\s*\]?", RegexOptions.IgnoreCase)]
    private static partial Regex RecipeLinkRegex();
}
