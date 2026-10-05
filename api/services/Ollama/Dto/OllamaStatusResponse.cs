using System.Text.Json.Serialization;

namespace api.Services.Ollama.Dto;

public class OllamaStatusResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
