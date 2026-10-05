namespace api.Data.Seeds;

public interface ISeeder
{
    Task SeedAsync(ApiDbContext context, CancellationToken cancellationToken = default);
}
