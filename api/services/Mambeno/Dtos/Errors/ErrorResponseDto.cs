namespace api.Services.Mambeno.Dtos.Errors;

/// <summary>WordPress REST API'ets fejlformat: { "code": "...", "message": "...", "data": { "status": 404 } }.</summary>
public sealed class ErrorResponseDto
{
    public string? Code { get; set; }
    public string? Message { get; set; }
}
