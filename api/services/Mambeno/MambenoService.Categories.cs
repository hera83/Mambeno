using api.Services.Mambeno.Dtos.Categories;

namespace api.Services.Mambeno;

public partial class MambenoService
{
    public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var (client, settings) = await CreateClientAsync(cancellationToken);
        using var _ = client;

        var categories = new List<CategoryDto>();
        var totalPages = 1;
        for (var page = 1; page <= totalPages; page++)
        {
            var endpoint = $"wp-json/wp/v2/categories?per_page={settings.PageSize}&page={page}&orderby=id&order=asc&_fields=id,name,slug,parent,count";
            using var response = await SendAsync(client, settings, endpoint, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            if (page == 1)
            {
                totalPages = ReadIntHeader(response, "X-WP-TotalPages") ?? 1;
            }

            categories.AddRange(await ReadJsonAsync<List<CategoryDto>>(response, cancellationToken));
        }

        return categories;
    }

    public async Task<bool> CategoryExistsAsync(int mambenoId, CancellationToken cancellationToken = default)
        => await ExistsAsync($"wp-json/wp/v2/categories/{mambenoId}?_fields=id", cancellationToken);

    private async Task<bool> ExistsAsync(string endpoint, CancellationToken cancellationToken)
    {
        var (client, settings) = await CreateClientAsync(cancellationToken);
        using var _ = client;

        using var response = await SendAsync(client, settings, endpoint, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        if (await IsWordPressNotFoundAsync(response, cancellationToken))
        {
            return false;
        }

        // Et uventet svar må aldrig tolkes som "findes ikke" - så kaster vi hellere.
        throw new MambenoException(response.StatusCode, $"Uventet svar fra Mambeno ved {endpoint}: status {(int)response.StatusCode}.");
    }

    private static int? ReadIntHeader(HttpResponseMessage response, string name)
        => response.Headers.TryGetValues(name, out var values) && int.TryParse(values.FirstOrDefault(), out var value)
            ? value
            : null;
}
