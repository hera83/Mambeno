using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using api.Services.Mambeno.Dtos.Recipes;

namespace api.Services.Mambeno;

public partial class MambenoService
{
    // Kun de felter vi bruger - holder svarene små (ingen HTML-indhold, Yoast-data eller billeder).
    private const string RecipeFields =
        "id,slug,link,title,date_gmt,modified_gmt,categories," +
        "meta.recipe_description,meta.recipe_prep_time,meta.recipe_cook_time," +
        "meta.recipe_servings,meta.recipe_servings_normalized,meta.recipe_servings_type," +
        "meta.recipe_notes,meta.recipe_ingredients,meta.recipe_instructions";

    public async Task<RecipeIndexDto> GetRecipeIndexAsync(CancellationToken cancellationToken = default)
    {
        var (client, settings) = await CreateClientAsync(cancellationToken);
        using var _ = client;

        var entries = new Dictionary<int, RecipeIndexEntryDto>();
        var reportedTotal = 0;
        var totalPages = 1;

        // Sorteret efter id, så nye opskrifter havner bagerst og ikke forskubber siderne undervejs.
        for (var page = 1; page <= totalPages; page++)
        {
            var endpoint = $"wp-json/wp/v2/recipe?per_page={settings.PageSize}&page={page}&orderby=id&order=asc&_fields=id,modified_gmt,categories";
            using var response = await SendAsync(client, settings, endpoint, cancellationToken);

            if (page > 1 && response.StatusCode == HttpStatusCode.BadRequest)
            {
                // Færre sider end ved start (fx en opskrift slettet undervejs) - indekset bliver
                // markeret som ufuldstændigt via ReportedTotal, så der ikke slettes på det grundlag.
                _logger.LogWarning("Mambeno havde færre opskriftssider end forventet (stoppede ved side {Page} af {TotalPages})", page, totalPages);
                break;
            }

            await EnsureSuccessAsync(response, cancellationToken);

            if (page == 1)
            {
                reportedTotal = ReadIntHeader(response, "X-WP-Total")
                                ?? throw new MambenoException(HttpStatusCode.BadGateway, "Mambeno sendte ikke X-WP-Total - kan ikke validere opskriftslisten.");
                totalPages = ReadIntHeader(response, "X-WP-TotalPages")
                             ?? throw new MambenoException(HttpStatusCode.BadGateway, "Mambeno sendte ikke X-WP-TotalPages - kan ikke validere opskriftslisten.");
            }

            foreach (var entry in await ReadJsonAsync<List<RecipeIndexEntryDto>>(response, cancellationToken))
            {
                entries[entry.Id] = entry;
            }
        }

        return new RecipeIndexDto
        {
            Entries = entries.Values.ToList(),
            ReportedTotal = reportedTotal
        };
    }

    public async IAsyncEnumerable<IReadOnlyList<RecipeDto>> GetRecipeBatchesAsync(
        IReadOnlyCollection<int> mambenoIds,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (client, settings) = await CreateClientAsync(cancellationToken);
        using var _ = client;

        foreach (var chunk in mambenoIds.Chunk(settings.PageSize))
        {
            var endpoint = $"wp-json/wp/v2/recipe?include={string.Join(',', chunk)}&per_page={chunk.Length}&orderby=include&_fields={RecipeFields}";
            var elements = await GetJsonAsync<List<JsonElement>>(client, settings, endpoint, cancellationToken);

            var recipes = new List<RecipeDto>(elements.Count);
            foreach (var element in elements)
            {
                try
                {
                    var recipe = element.Deserialize<RecipeDto>(JsonOptions);
                    if (recipe is not null)
                    {
                        recipes.Add(recipe);
                    }
                }
                catch (JsonException ex)
                {
                    var id = element.TryGetProperty("id", out var idElement) ? idElement.ToString() : "?";
                    _logger.LogWarning(ex, "Kunne ikke læse Mambeno-opskrift {MambenoId} - springes over i denne kørsel", id);
                }
            }

            yield return recipes;
        }
    }

    public async Task<bool> RecipeExistsAsync(int mambenoId, CancellationToken cancellationToken = default)
        => await ExistsAsync($"wp-json/wp/v2/recipe/{mambenoId}?_fields=id", cancellationToken);

    public async Task<RecipePageDataDto> GetRecipePageDataAsync(string recipeUrl, CancellationToken cancellationToken = default)
    {
        var (client, settings) = await CreateClientAsync(cancellationToken);
        using var _ = client;

        if (!Uri.TryCreate(recipeUrl, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Host, client.BaseAddress!.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new MambenoException(HttpStatusCode.BadRequest, $"Opskrifts-URL'en ligger ikke på {client.BaseAddress!.Host}: {recipeUrl}");
        }

        using var response = await SendAsync(client, settings, uri.ToString(), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        return new RecipePageDataDto
        {
            Ingredients = ParsePageIngredients(html),
            Instructions = ParsePageInstructions(html)
        };
    }

    /// <summary>Siden indeholder samme ingrediensdata som API'et i et script: window.recipeIngredients = [...];</summary>
    private static List<RecipeIngredientDto>? ParsePageIngredients(string html)
    {
        var match = PageIngredientsRegex().Match(html);
        if (!match.Success)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<RecipeIngredientDto>>(match.Groups[1].Value, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Fremgangsmåden ligger som HowToStep'er i sidens schema.org Recipe JSON-LD.</summary>
    private static List<RecipeInstructionDto>? ParsePageInstructions(string html)
    {
        foreach (Match match in JsonLdRegex().Matches(html))
        {
            try
            {
                using var document = JsonDocument.Parse(match.Groups[1].Value);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("@type", out var type) || type.GetString() != "Recipe"
                    || !root.TryGetProperty("recipeInstructions", out var steps) || steps.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                var instructions = new List<RecipeInstructionDto>();
                foreach (var step in steps.EnumerateArray())
                {
                    var text = step.ValueKind switch
                    {
                        JsonValueKind.String => step.GetString(),
                        JsonValueKind.Object when step.TryGetProperty("text", out var stepText) => stepText.GetString(),
                        _ => null
                    };

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        instructions.Add(new RecipeInstructionDto { Description = text });
                    }
                }

                return instructions;
            }
            catch (JsonException)
            {
                // Ugyldig JSON-LD - prøv den næste blok.
            }
        }

        return null;
    }

    [GeneratedRegex(@"window\.recipeIngredients\s*=\s*(\[.*\])\s*;")]
    private static partial Regex PageIngredientsRegex();

    [GeneratedRegex(@"<script[^>]*type=""application/ld\+json""[^>]*>(.*?)</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex JsonLdRegex();
}
