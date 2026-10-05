using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace api.Services.Mambeno;

/// <summary>
/// Læser en værdi som tekst uanset om WordPress har gemt den som tekst, tal, bool eller null
/// (PHP-meta er løst typet, så fx en ingrediens' "amount" kan være både "250" og 250).
/// </summary>
public sealed class FlexibleStringJsonConverter : JsonConverter<string>
{
    public override bool HandleNull => true;

    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString() ?? string.Empty;
            case JsonTokenType.Number:
                return reader.TryGetInt64(out var integer)
                    ? integer.ToString(CultureInfo.InvariantCulture)
                    : reader.GetDouble().ToString(CultureInfo.InvariantCulture);
            case JsonTokenType.True:
                return "true";
            case JsonTokenType.False:
            case JsonTokenType.Null:
                return string.Empty;
            default:
                reader.Skip();
                return string.Empty;
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
