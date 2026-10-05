using api.Dtos.Logs;
using api.Services.Logging.Interfaces;
using Microsoft.Data.Sqlite;

namespace api.Services.Logging;

public class LogQueryService(LogDatabaseSettings settings) : ILogQueryService
{
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    public async Task<LogSearchResponseDto> SearchAsync(
        LogSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(settings.DatabasePath))
        {
            return new LogSearchResponseDto { Page = request.Page, PageSize = request.PageSize };
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 500);

        var filters = new List<(string Sql, string Name, object Value)>();

        if (!string.IsNullOrWhiteSpace(request.Level))
        {
            filters.Add(("Level = @level", "@level", request.Level));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            filters.Add(("RenderedMessage LIKE @search", "@search", $"%{request.Search}%"));
        }

        if (request.From.HasValue)
        {
            filters.Add(("Timestamp >= @from", "@from", request.From.Value.ToString(TimestampFormat)));
        }

        if (request.To.HasValue)
        {
            filters.Add(("Timestamp <= @to", "@to", request.To.Value.ToString(TimestampFormat)));
        }

        var whereSql = filters.Count > 0
            ? "WHERE " + string.Join(" AND ", filters.Select(f => f.Sql))
            : string.Empty;

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = settings.DatabasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ConnectionString;

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var totalCount = await GetTotalCountAsync(connection, whereSql, filters, cancellationToken);
        var items = await GetPageAsync(connection, whereSql, filters, page, pageSize, cancellationToken);

        return new LogSearchResponseDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static async Task<long> GetTotalCountAsync(
        SqliteConnection connection,
        string whereSql,
        List<(string Sql, string Name, object Value)> filters,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM \"{LogDatabaseSettings.TableName}\" {whereSql}";
        AddParameters(cmd, filters);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static async Task<List<LogEntryDto>> GetPageAsync(
        SqliteConnection connection,
        string whereSql,
        List<(string Sql, string Name, object Value)> filters,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            $"SELECT id, Timestamp, Level, Exception, RenderedMessage, Properties " +
            $"FROM \"{LogDatabaseSettings.TableName}\" {whereSql} " +
            "ORDER BY id DESC LIMIT @take OFFSET @skip";
        AddParameters(cmd, filters);
        cmd.Parameters.AddWithValue("@take", pageSize);
        cmd.Parameters.AddWithValue("@skip", (page - 1) * pageSize);

        var items = new List<LogEntryDto>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new LogEntryDto
            {
                Id = reader.GetInt64(0),
                Timestamp = DateTime.SpecifyKind(DateTime.Parse(reader.GetString(1)), DateTimeKind.Utc),
                Level = reader.GetString(2),
                Exception = reader.IsDBNull(3) ? null : reader.GetString(3),
                Message = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Properties = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return items;
    }

    private static void AddParameters(
        SqliteCommand cmd,
        List<(string Sql, string Name, object Value)> filters)
    {
        foreach (var filter in filters)
        {
            cmd.Parameters.AddWithValue(filter.Name, filter.Value);
        }
    }
}
