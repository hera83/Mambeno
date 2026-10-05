namespace api.Services.RecipeSync;

/// <summary>Bindes fra konfigurationssektionen "RecipeSync".</summary>
public class RecipeSyncSettings
{
    public bool Enabled { get; set; } = true;

    /// <summary>Lokalt tidspunkt (i <see cref="TimeZone"/>) hvor den daglige sync kører - midt om natten, når Mambeno har mindst trafik.</summary>
    public TimeSpan DailyRunTime { get; set; } = new(3, 30, 0);

    public string TimeZone { get; set; } = "Europe/Copenhagen";

    /// <summary>Ventetid før et nyt forsøg, hvis en sync fejler.</summary>
    public int RetryDelayMinutes { get; set; } = 60;

    /// <summary>
    /// Sikkerhedsgrænse: hvis en kørsel vil slette flere end denne procentdel af de gemte opskrifter
    /// (eller kategorier), slettes der intet, og kørslen logger en fejl i stedet.
    /// </summary>
    public double MaxDeletionPercent { get; set; } = 5;

    /// <summary>Antal sletninger der altid er tilladt pr. kørsel, uanset procentgrænsen (relevant når der er få rækker, fx kategorier).</summary>
    public int MaxDeletionsAlwaysAllowed { get; set; } = 10;

    /// <summary>
    /// Antal ekstra opskrifter der hentes igen pr. kørsel, selvom Mambeno ikke har markeret dem som
    /// ændret (de længst siden hentede først). Fanger ændringer der ikke opdaterer ændringsdatoen, så
    /// hele databasen genopfriskes i løbet af ca. antal opskrifter / værdi dage. 0 slår det fra.
    /// </summary>
    public int RefreshPerRun { get; set; } = 100;

    /// <summary>GET /health reports "Unhealthy" when the last successful sync is older than this many days.</summary>
    public int MaxDataAgeDays { get; set; } = 7;

    public static RecipeSyncSettings Load(IConfiguration configuration)
        => configuration.GetSection("RecipeSync").Get<RecipeSyncSettings>() ?? new RecipeSyncSettings();

    /// <summary>Hvor mange rækker en kørsel højst må slette ud af <paramref name="existingCount"/>.</summary>
    public int GetAllowedDeletions(int existingCount)
        => Math.Max(MaxDeletionsAlwaysAllowed, (int)Math.Floor(existingCount * MaxDeletionPercent / 100));
}
