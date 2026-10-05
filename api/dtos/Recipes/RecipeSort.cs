using System.Text.Json.Serialization;

namespace api.Dtos.Recipes;

/// <summary>Sortering af opskriftslisten.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecipeSort>))]
public enum RecipeSort
{
    /// <summary>Alfabetisk efter titel.</summary>
    Title,

    /// <summary>Senest ændret først.</summary>
    LastModified,

    /// <summary>Kortest samlet tid først.</summary>
    TotalTime
}
