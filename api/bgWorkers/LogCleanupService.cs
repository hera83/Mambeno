using api.Services.Logging;
using Microsoft.Data.Sqlite;

namespace api.BgWorkers;

/// <summary>
/// Keeps the Serilog SQLite log database under <see cref="LogDatabaseSettings.MaxSizeBytes"/> by
/// deleting the oldest rows and reclaiming disk space, instead of relying on the sink's rollover.
/// </summary>
public class LogCleanupService(LogDatabaseSettings settings, ILogger<LogCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);
    private const int MaxCleanupIterationsPerCheck = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        try
        {
            do
            {
                try
                {
                    await EnforceSizeLimitAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Expected when the host is shutting down mid-check.
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to enforce log database size limit");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Expected when the host is shutting down; PeriodicTimer.WaitForNextTickAsync
            // throws instead of returning false once stoppingToken is cancelled.
        }
    }

    private async Task EnforceSizeLimitAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.DatabasePath))
        {
            return;
        }

        await using var connection = new SqliteConnection($"Data Source={settings.DatabasePath}");
        await connection.OpenAsync(cancellationToken);
        await SetBusyTimeoutAsync(connection, cancellationToken);

        for (var i = 0; i < MaxCleanupIterationsPerCheck; i++)
        {
            await CheckpointAsync(connection, cancellationToken);

            var sizeBytes = new FileInfo(settings.DatabasePath).Length;
            if (sizeBytes <= LogDatabaseSettings.MaxSizeBytes)
            {
                return;
            }

            var rowCount = await GetRowCountAsync(connection, cancellationToken);
            if (rowCount == 0)
            {
                return;
            }

            var deleteCount = Math.Max(1000, rowCount / 10);
            await DeleteOldestAsync(connection, deleteCount, cancellationToken);
            await VacuumAsync(connection, cancellationToken);

            logger.LogWarning(
                "Log database exceeded {MaxSizeBytes} bytes (was {ActualSizeBytes} bytes); deleted the {DeleteCount} oldest rows",
                LogDatabaseSettings.MaxSizeBytes, sizeBytes, deleteCount);
        }
    }

    private static async Task SetBusyTimeoutAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA busy_timeout = 5000;";
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task CheckpointAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<long> GetRowCountAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM \"{LogDatabaseSettings.TableName}\"";
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static async Task DeleteOldestAsync(SqliteConnection connection, long count, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            $"DELETE FROM \"{LogDatabaseSettings.TableName}\" WHERE id IN " +
            $"(SELECT id FROM \"{LogDatabaseSettings.TableName}\" ORDER BY id ASC LIMIT @count)";
        cmd.Parameters.AddWithValue("@count", count);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task VacuumAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "VACUUM";
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
