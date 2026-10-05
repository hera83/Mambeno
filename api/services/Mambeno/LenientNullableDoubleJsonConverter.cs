using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace api.Services.Mambeno;

/// <summary>
/// Læser et tal, der hos WordPress kan være gemt som tal, tekst ("35"), tom tekst eller null.
/// Alt der ikke kan tolkes som et tal bliver til null i stedet for at kaste.
/// </summary>
public sealed class LenientNullableDoubleJsonConverter : JsonConverter<double?>
{
    public override bool HandleNull => true;

    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetDouble();
            case JsonTokenType.String:
                var text = reader.GetString()?.Trim().Replace(',', '.');
                return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;
            case JsonTokenType.Null:
            case JsonTokenType.True:
            case JsonTokenType.False:
                return null;
            default:
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}
