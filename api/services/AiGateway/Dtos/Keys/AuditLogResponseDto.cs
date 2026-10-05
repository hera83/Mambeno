using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Keys;

public class AuditLogResponseDto
{
    [JsonPropertyName("entries")]
    public List<AuditLogEntryResponseDto>? Entries { get; set; }
}
