using api.Dtos.Logs;
using api.Services.Authentication;
using api.Services.Logging.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Policy = AuthorizationPolicies.MasterKeyOnly)]
public class LogController(ILogQueryService logQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<LogSearchResponseDto>> Search(
        [FromQuery] LogSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await logQueryService.SearchAsync(request, cancellationToken);
        return Ok(result);
    }
}
