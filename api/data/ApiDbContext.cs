using api.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Data;

public class ApiDbContext(DbContextOptions<ApiDbContext> options) : DbContext(options)
{
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeSyncRun> RecipeSyncRuns => Set<RecipeSyncRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(k => k.KeyHash).IsUnique();
            entity.Property(k => k.Name).HasMaxLength(200);
            entity.Property(k => k.ContactName).HasMaxLength(200);
            entity.Property(k => k.ContactNote).HasMaxLength(1000);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.MambenoId).IsUnique();
            entity.Property(c => c.Name).HasMaxLength(200);
            entity.Property(c => c.Slug).HasMaxLength(200);
            entity.Property(c => c.Icon).HasMaxLength(100);
            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasIndex(r => r.MambenoId).IsUnique();
            entity.HasIndex(r => r.LastFetchedAt);
            entity.Property(r => r.Title).HasMaxLength(500);
            entity.Property(r => r.Slug).HasMaxLength(500);
            entity.Property(r => r.SourceUrl).HasMaxLength(1000);
            entity.Property(r => r.ServingsDescription).HasMaxLength(200);
            entity.HasMany(r => r.Categories)
                .WithMany(c => c.Recipes)
                .UsingEntity(
                    "RecipeCategories",
                    category => category.HasOne(typeof(Category)).WithMany().HasForeignKey("CategoryId").OnDelete(DeleteBehavior.Cascade),
                    recipe => recipe.HasOne(typeof(Recipe)).WithMany().HasForeignKey("RecipeId").OnDelete(DeleteBehavior.Cascade),
                    join => join.HasKey("RecipeId", "CategoryId"));
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasIndex(i => new { i.RecipeId, i.SortOrder });
            entity.Property(i => i.GroupName).HasMaxLength(200);
            entity.Property(i => i.Amount).HasMaxLength(50);
            entity.Property(i => i.Unit).HasMaxLength(100);
            entity.Property(i => i.Name).HasMaxLength(500);
            entity.Property(i => i.Notes).HasMaxLength(1000);
            entity.HasOne(i => i.Recipe)
                .WithMany(r => r.Ingredients)
                .HasForeignKey(i => i.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecipeStep>(entity =>
        {
            entity.HasIndex(s => new { s.RecipeId, s.SortOrder });
            entity.Property(s => s.GroupName).HasMaxLength(200);
            entity.HasOne(s => s.Recipe)
                .WithMany(r => r.Steps)
                .HasForeignKey(s => s.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecipeSyncRun>(entity =>
        {
            entity.HasIndex(r => r.StartedAt);
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.Message).HasMaxLength(4000);
        });
    }
}
