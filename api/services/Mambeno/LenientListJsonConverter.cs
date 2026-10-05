using System.Text.Json;
using System.Text.Json.Serialization;

namespace api.Services.Mambeno;

/// <summary>
/// Læser et JSON-array som en liste, men returnerer null (i stedet for at kaste) hvis WordPress har
/// gemt noget andet end et array i feltet - så én skæv opskrift ikke vælter en hel batch.
/// </summary>
public sealed class LenientListJsonConverter<T> : JsonConverter<List<T>?>
{
    public override bool HandleNull => true;

    public override List<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            reader.Skip();
            return null;
        }

        var list = new List<T>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                var item = JsonSerializer.Deserialize<T>(ref reader, options);
                if (item is not null)
                {
                    list.Add(item);
                }
            }
            else
            {
                reader.Skip();
            }
        }

        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<T>? value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, options);
}
