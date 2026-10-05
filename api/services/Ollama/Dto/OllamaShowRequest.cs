using System.Text.Json.Serialization;

namespace api.Services.Ollama.Dto;

public class OllamaShowRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("verbose")]
    public bool? Verbose { get; set; }
}
