using api.Data;
using api.Data.Models;
using api.Dtos.Health;
using api.Services.RecipeSync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class HealthController(ApiDbContext dbContext, IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Get the API status, including when the recipe data was last synced from Mambeno.")]
    [EndpointDescription("Returns 503 with status \"Unhealthy\" if the recipe data has never been synced or the last successful sync is older than RecipeSync:MaxDataAgeDays (default 7 days).")]
    [ProducesResponseType<HealthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthResponseDto>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthResponseDto>> Get(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var maxDataAgeDays = RecipeSyncSettings.Load(configuration).MaxDataAgeDays;

        var lastSuccessfulSync = await dbContext.RecipeSyncRuns
            .Where(r => r.Status == RecipeSyncStatus.Succeeded)
            .OrderByDescending(r => r.StartedAt)
            .Select(r => r.FinishedAt ?? r.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
        DateTime? dataLastUpdatedUtc = lastSuccessfulSync == default
            ? null
            : DateTime.SpecifyKind(lastSuccessfulSync, DateTimeKind.Utc);

        var message = dataLastUpdatedUtc switch
        {
            null => "The recipe data has never been synced from Mambeno.",
            { } updated when now - updated > TimeSpan.FromDays(maxDataAgeDays) =>
                $"The recipe data is older than {maxDataAgeDays} days (last synced {updated:u}).",
            _ => null
        };

        var response = new HealthResponseDto
        {
            Status = message is null ? "Healthy" : "Unhealthy",
            TimestampUtc = now,
            DataLastUpdatedUtc = dataLastUpdatedUtc,
            Message = message
        };

        return message is null ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
