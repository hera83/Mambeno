using api.Dtos.Categories;
using api.Services.Recipes.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

/// <summary>Kategorierne spejlet fra Mambeno. Stier og svar har samme form som Mors Opskrifter-API'et.</summary>
[ApiController]
[Route("api/v1/categories")]
[Tags("Categories")]
public class CategoriesController(IRecipeQueryService recipeQueryService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Get all categories with their recipe count.")]
    public async Task<ActionResult<List<CategoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await recipeQueryService.GetCategoriesAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Get a single category.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<CategoryDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var category = await recipeQueryService.GetCategoryAsync(id, cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }
}
