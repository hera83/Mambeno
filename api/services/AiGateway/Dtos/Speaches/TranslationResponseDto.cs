using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Speaches;

public class TranslationResponseDto
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("duration")]
    public double? Duration { get; set; }

    [JsonPropertyName("segments")]
    public List<TranscriptionSegmentDto>? Segments { get; set; }
}
