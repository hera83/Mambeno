using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Speaches;

public class ListModelsResponseDto
{
    [JsonPropertyName("data")]
    public List<ModelDto>? Data { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }
}
