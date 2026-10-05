using api.Dtos.Logs;

namespace api.Services.Logging.Interfaces;

public interface ILogQueryService
{
    Task<LogSearchResponseDto> SearchAsync(LogSearchRequestDto request, CancellationToken cancellationToken = default);
}
