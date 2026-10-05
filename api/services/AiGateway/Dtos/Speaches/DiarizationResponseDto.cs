using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Speaches;

public class DiarizationResponseDto
{
    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    [JsonPropertyName("segments")]
    public List<DiarizationSegmentDto>? Segments { get; set; }
}
