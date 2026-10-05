using api.Dtos.Recipes;
using api.Services.Recipes.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

/// <summary>Opskrifterne spejlet fra Mambeno. Stier, filtre og svar har samme form som Mors Opskrifter-API'et.</summary>
[ApiController]
[Route("api/v1/recipes")]
[Tags("Recipes")]
public class RecipesController(IRecipeQueryService recipeQueryService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Search and filter recipes.")]
    [EndpointDescription("All filters are combined (AND). Text search is case-insensitive for a-z.")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<RecipeSearchResponseDto>> Search(
        [FromQuery] RecipeSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await recipeQueryService.SearchRecipesAsync(request, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Get a single recipe.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<RecipeDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var recipe = await recipeQueryService.GetRecipeAsync(id, cancellationToken);
        return recipe is null ? NotFound() : Ok(recipe);
    }
}
