using api.Data;
using api.Data.Models;
using api.Services.RecipeSync;
using api.Services.RecipeSync.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace api.BgWorkers;

/// <summary>
/// Kører <see cref="IRecipeSyncService"/> én gang i døgnet på <see cref="RecipeSyncSettings.DailyRunTime"/>.
/// Har databasen aldrig været synkroniseret (eller er et tidspunkt blevet sprunget over, fx fordi appen
/// var slukket), kører den med det samme. Fejler en kørsel, prøves igen efter <see cref="RecipeSyncSettings.RetryDelayMinutes"/>.
/// Tidsplanen udledes af RecipeSyncRuns-tabellen, så den overlever genstarter.
/// </summary>
public class RecipeSyncWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<RecipeSyncWorker> logger) : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);

    // En kørsel lige efter en succesfuld kørsel giver ingen mening; næste planlagte tidspunkt skal ligge mindst så langt efter.
    private static readonly TimeSpan MinimumIntervalBetweenRuns = TimeSpan.FromHours(12);

    // Vågn mindst så ofte op og regn tidsplanen igen (robust over for sommertid, dvale og ændret konfiguration).
    private static readonly TimeSpan MaxSleep = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);

            DateTime? announcedRunUtc = null;
            while (!stoppingToken.IsCancellationRequested)
            {
                var settings = RecipeSyncSettings.Load(configuration);
                if (!settings.Enabled)
                {
                    logger.LogInformation("Opskrifts-sync er slået fra (RecipeSync:Enabled = false); workeren stopper");
                    return;
                }

                var nextRunUtc = await GetNextRunUtcAsync(settings, stoppingToken);
                var wait = nextRunUtc - DateTime.UtcNow;
                if (wait > TimeSpan.Zero)
                {
                    if (announcedRunUtc != nextRunUtc)
                    {
                        logger.LogInformation("Næste opskrifts-sync fra Mambeno: {NextRunUtc:u}", nextRunUtc);
                        announcedRunUtc = nextRunUtc;
                    }

                    await Task.Delay(wait < MaxSleep ? wait : MaxSleep, stoppingToken);
                    continue;
                }

                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<IRecipeSyncService>();
                    await syncService.SyncAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // SyncAsync gemmer selv sine fejl; dette fanger kun fejl før kørslen overhovedet kom i gang.
                    logger.LogError(ex, "Opskrifts-sync kunne ikke startes; prøver igen om {RetryDelayMinutes} min.", settings.RetryDelayMinutes);
                    await Task.Delay(TimeSpan.FromMinutes(settings.RetryDelayMinutes), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Forventet når appen lukker ned.
        }
    }

    private async Task<DateTime> GetNextRunUtcAsync(RecipeSyncSettings settings, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

        var lastSuccessUtc = await db.RecipeSyncRuns
            .Where(r => r.Status == RecipeSyncStatus.Succeeded)
            .OrderByDescending(r => r.StartedAt)
            .Select(r => (DateTime?)r.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var lastAttempt = await db.RecipeSyncRuns
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Aldrig kørt: nu. Ellers: første planlagte tidspunkt mindst 12 timer efter sidste succes - ligger
        // det i fortiden (appen var slukket), bliver ventetiden negativ, og der køres med det samme.
        var nextRunUtc = lastSuccessUtc is null
            ? DateTime.UtcNow
            : GetNextScheduledUtc(DateTime.SpecifyKind(lastSuccessUtc.Value, DateTimeKind.Utc) + MinimumIntervalBetweenRuns, settings);

        if (lastAttempt is { Status: RecipeSyncStatus.Failed })
        {
            var retryAtUtc = DateTime.SpecifyKind(lastAttempt.StartedAt, DateTimeKind.Utc) + TimeSpan.FromMinutes(settings.RetryDelayMinutes);
            if (retryAtUtc > nextRunUtc)
            {
                nextRunUtc = retryAtUtc;
            }
        }

        return nextRunUtc;
    }

    private DateTime GetNextScheduledUtc(DateTime afterUtc, RecipeSyncSettings settings)
    {
        var timeZone = ResolveTimeZone(settings.TimeZone);
        var afterLocal = TimeZoneInfo.ConvertTimeFromUtc(afterUtc, timeZone);

        var candidate = afterLocal.Date + settings.DailyRunTime;
        if (candidate <= afterLocal)
        {
            candidate = candidate.AddDays(1);
        }

        if (timeZone.IsInvalidTime(candidate))
        {
            // Tidspunktet findes ikke den dag (sommertidsskift) - tag timen efter.
            candidate = candidate.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(candidate, timeZone);
    }

    private TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            logger.LogWarning("Ukendt tidszone '{TimeZone}' i RecipeSync:TimeZone - bruger serverens lokale tidszone", timeZoneId);
            return TimeZoneInfo.Local;
        }
    }
}
