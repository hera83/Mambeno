namespace api.Dtos.Logs;

public class LogSearchRequestDto
{
    /// <summary>Filter by exact Serilog level (e.g. Information, Warning, Error).</summary>
    public string? Level { get; set; }

    /// <summary>Case-insensitive substring match against the rendered log message.</summary>
    public string? Search { get; set; }

    /// <summary>Inclusive lower bound, in UTC (log timestamps are stored as UTC).</summary>
    public DateTime? From { get; set; }

    /// <summary>Inclusive upper bound, in UTC (log timestamps are stored as UTC).</summary>
    public DateTime? To { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
