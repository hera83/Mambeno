namespace api.Services.Mambeno;

public class MambenoSettings
{
    public string BaseUrl { get; set; } = "https://mambeno.dk";
    public string UserAgent { get; set; } = "Mozilla/5.0 (compatible; MambenoRecipeSync/1.0)";
    public int RequestTimeoutSeconds { get; set; } = 60;

    /// <summary>Mindste pause mellem to kald til Mambeno, så vi aldrig belaster siden.</summary>
    public int DelayBetweenRequestsMilliseconds { get; set; } = 1500;

    /// <summary>Antal opskrifter pr. kald (WordPress' maksimum er 100).</summary>
    public int PageSize { get; set; } = 100;

    /// <summary>Antal genforsøg ved midlertidige fejl (timeout, 429, 5xx).</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Første ventetid før genforsøg; fordobles for hvert forsøg (medmindre Mambeno sender Retry-After).</summary>
    public int RetryBaseDelaySeconds { get; set; } = 30;
}
