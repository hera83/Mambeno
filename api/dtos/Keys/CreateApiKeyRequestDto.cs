using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Keys;

public class CreateApiKeyRequestDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ContactName { get; set; }

    [StringLength(1000)]
    public string? ContactNote { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
