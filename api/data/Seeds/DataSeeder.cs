namespace api.Data.Seeds;

public static class DataSeeder
{
    private static readonly ISeeder[] Seeders = [];

    public static async Task SeedAsync(ApiDbContext context, CancellationToken cancellationToken = default)
    {
        foreach (var seeder in Seeders)
        {
            await seeder.SeedAsync(context, cancellationToken);
        }
    }
}
