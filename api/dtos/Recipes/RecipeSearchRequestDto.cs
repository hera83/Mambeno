using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace api.Dtos.Recipes;

/// <summary>Query-parametre til GET /api/v1/recipes - samme navne og betydning som i Mors Opskrifter.</summary>
public class RecipeSearchRequestDto
{
    [Description("Free text in title, description, category, notes or ingredient names.")]
    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [Description("Exact category name.")]
    [FromQuery(Name = "category")]
    public string? Category { get; set; }

    [Description("Category id.")]
    [FromQuery(Name = "categoryId")]
    public int? CategoryId { get; set; }

    [Description("Difficulty. Mambeno doesn't specify a difficulty, so this filter always returns an empty list.")]
    [FromQuery(Name = "difficulty")]
    public Difficulty? Difficulty { get; set; }

    [Description("Max total time (kitchen time + waiting time) in minutes. Recipes without a specified time (0 min.) are excluded.")]
    [FromQuery(Name = "maxTotalMinutes")]
    public int? MaxTotalMinutes { get; set; }

    [Description("The recipe must contain an ingredient whose name contains the text. Can be specified multiple times (all must match).")]
    [FromQuery(Name = "ingredient")]
    public List<string> Ingredient { get; set; } = [];

    [Description("Only recipes modified at or after this time (UTC if no time zone is given) — useful for synchronization.")]
    [FromQuery(Name = "modifiedSince")]
    public DateTime? ModifiedSince { get; set; }

    [Description("Sort order.")]
    [FromQuery(Name = "sort")]
    public RecipeSort Sort { get; set; } = RecipeSort.Title;

    [Description("Page number (starts at 1).")]
    [FromQuery(Name = "page")]
    public int Page { get; set; } = 1;

    [Description("Items per page (1–200).")]
    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 50;
}
