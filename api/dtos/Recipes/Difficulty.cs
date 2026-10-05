using System.Text.Json.Serialization;

namespace api.Dtos.Recipes;

/// <summary>Sværhedsgrad som i Mors Opskrifter. Mambeno angiver ikke sværhedsgrad, så den er altid null her.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Difficulty>))]
public enum Difficulty
{
    Let,
    Middel,
    Svær
}
