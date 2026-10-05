namespace api.Dtos.Health;

public class HealthResponseDto
{
    public string Status { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }

    /// <summary>When the recipe data was last successfully synced from Mambeno, or null if it never has been.</summary>
    public DateTime? DataLastUpdatedUtc { get; set; }

    /// <summary>Why the status is "Unhealthy"; null when healthy.</summary>
    public string? Message { get; set; }
}
