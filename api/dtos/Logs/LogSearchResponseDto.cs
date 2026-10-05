namespace api.Dtos.Logs;

public class LogSearchResponseDto
{
    public List<LogEntryDto> Items { get; set; } = [];
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
