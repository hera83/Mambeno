namespace api.Data.Models;

public class ApiKey
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string KeyPreview { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? ContactName { get; set; }
    public string? ContactNote { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RolledOverAt { get; set; }
}
