namespace api.Services.Logging;

public class LogDatabaseSettings(string databasePath)
{
    public const string TableName = "Logs";
    public const long MaxSizeBytes = 1024L * 1024 * 1024; // 1 GiB

    public string DatabasePath { get; } = databasePath;
}
