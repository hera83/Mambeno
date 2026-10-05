using api.Data;
using api.Data.Models;
using api.Dtos.Keys;
using api.Services.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("[controller]/[action]")]
[Authorize(Policy = AuthorizationPolicies.MasterKeyOnly)]
public class KeysController(ApiDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiKeyResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var keys = await dbContext.ApiKeys
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => ToResponseDto(k))
            .ToListAsync(cancellationToken);

        return Ok(keys);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiKeyResponseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var key = await dbContext.ApiKeys.FindAsync([id], cancellationToken);
        if (key is null)
        {
            return NotFound();
        }

        return Ok(ToResponseDto(key));
    }

    [HttpPost]
    public async Task<ActionResult<ApiKeyCreatedResponseDto>> Create(
        CreateApiKeyRequestDto request,
        CancellationToken cancellationToken)
    {
        var rawKey = ApiKeyGenerator.GenerateKey();

        var apiKey = new ApiKey
        {
            Name = request.Name,
            ContactName = request.ContactName,
            ContactNote = request.ContactNote,
            ExpiresAt = request.ExpiresAt,
            KeyHash = ApiKeyGenerator.Hash(rawKey),
            KeyPreview = ApiKeyGenerator.Preview(rawKey)
        };

        dbContext.ApiKeys.Add(apiKey);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = ToCreatedResponseDto(apiKey, rawKey);
        return CreatedAtAction(nameof(GetById), new { id = apiKey.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiKeyResponseDto>> Update(
        int id,
        UpdateApiKeyRequestDto request,
        CancellationToken cancellationToken)
    {
        var key = await dbContext.ApiKeys.FindAsync([id], cancellationToken);
        if (key is null)
        {
            return NotFound();
        }

        key.Name = request.Name;
        key.ContactName = request.ContactName;
        key.ContactNote = request.ContactNote;
        key.ExpiresAt = request.ExpiresAt;
        key.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponseDto(key));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var key = await dbContext.ApiKeys.FindAsync([id], cancellationToken);
        if (key is null)
        {
            return NotFound();
        }

        dbContext.ApiKeys.Remove(key);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:int}/rollover")]
    public async Task<ActionResult<ApiKeyCreatedResponseDto>> Rollover(int id, CancellationToken cancellationToken)
    {
        var key = await dbContext.ApiKeys.FindAsync([id], cancellationToken);
        if (key is null)
        {
            return NotFound();
        }

        var rawKey = ApiKeyGenerator.GenerateKey();
        key.KeyHash = ApiKeyGenerator.Hash(rawKey);
        key.KeyPreview = ApiKeyGenerator.Preview(rawKey);
        key.RolledOverAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToCreatedResponseDto(key, rawKey));
    }

    private static ApiKeyResponseDto ToResponseDto(ApiKey key) => new()
    {
        Id = key.Id,
        Name = key.Name,
        KeyPreview = key.KeyPreview,
        IsActive = key.IsActive,
        ContactName = key.ContactName,
        ContactNote = key.ContactNote,
        ExpiresAt = key.ExpiresAt,
        CreatedAt = key.CreatedAt,
        RolledOverAt = key.RolledOverAt
    };

    private static ApiKeyCreatedResponseDto ToCreatedResponseDto(ApiKey key, string rawKey) => new()
    {
        Id = key.Id,
        Name = key.Name,
        KeyPreview = key.KeyPreview,
        IsActive = key.IsActive,
        ContactName = key.ContactName,
        ContactNote = key.ContactNote,
        ExpiresAt = key.ExpiresAt,
        CreatedAt = key.CreatedAt,
        RolledOverAt = key.RolledOverAt,
        Key = rawKey
    };
}
