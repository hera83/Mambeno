namespace api.Dtos.Keys;

public class ApiKeyResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPreview { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ContactName { get; set; }
    public string? ContactNote { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RolledOverAt { get; set; }
}
