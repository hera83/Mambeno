using api.BgWorkers;
using api.Data;
using api.Data.Seeds;
using api.Services.AiGateway;
using api.Services.AiGateway.Interfaces;
using api.Services.Authentication;
using api.Services.ErrorHandling;
using api.Services.Logging;
using api.Services.Logging.Interfaces;
using api.Services.Mail;
using api.Services.Mail.Interfaces;
using api.Services.Mambeno;
using api.Services.Mambeno.Interfaces;
using api.Services.Ollama;
using api.Services.Ollama.Interfaces;
using api.Services.Recipes;
using api.Services.Recipes.Interfaces;
using api.Services.RecipeSync;
using api.Services.RecipeSync.Interfaces;
using api.Services.Sms;
using api.Services.Sms.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var apiName = builder.Configuration["Api:Name"] ?? "api";

var environmentName = builder.Environment.EnvironmentName;
var environmentBadgeColor = environmentName switch
{
    "Production" => "brightgreen",
    "Development" => "orange",
    _ => "yellow"
};
var environmentBadgeMarkdown =
    $"![{environmentName}](https://img.shields.io/badge/env-{Uri.EscapeDataString(environmentName)}-{environmentBadgeColor})";

var logDbPath = Path.Combine(builder.Environment.ContentRootPath, "app_dbs", "log.db");
Directory.CreateDirectory(Path.GetDirectoryName(logDbPath)!);

builder.Host.UseSerilog((_, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        // Hver SQL-kommando logges ellers på Information - under opskrifts-sync'en bliver det til tusindvis af rækker i log.db.
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.SQLite(
            sqliteDbPath: logDbPath,
            tableName: LogDatabaseSettings.TableName,
            storeTimestampInUtc: true,
            maxDatabaseSize: (uint)(LogDatabaseSettings.MaxSizeBytes / (1024 * 1024)),
            rollOver: false);
});

builder.Services.AddSingleton(new LogDatabaseSettings(logDbPath));
builder.Services.AddScoped<ILogQueryService, LogQueryService>();
builder.Services.AddHostedService<LogCleanupService>();

builder.Services.AddControllers();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ApiKeySecuritySchemeTransformer>();
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = apiName;
        document.Info.Description = environmentBadgeMarkdown;
        document.Servers?.Clear();
        return Task.CompletedTask;
    });
});

builder.Services.AddHttpClient();

builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.Scheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.Scheme, null);

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(AuthorizationPolicies.MasterKeyOnly, policy =>
        policy.RequireClaim(ApiKeyClaimTypes.KeyType, ApiKeyClaimTypes.Master));
});

builder.Services.AddScoped<IOllamaConfigurationProvider, OllamaConfigurationProvider>();
builder.Services.AddScoped<OllamaHttpClientFactory>();
builder.Services.AddScoped<IOllamaService, OllamaService>();

builder.Services.AddScoped<IAiGatewayConfigurationProvider, AiGatewayConfigurationProvider>();
builder.Services.AddScoped<AiGatewayHttpClientFactory>();
builder.Services.AddScoped<IAiGatewayService, AiGatewayService>();

builder.Services.AddScoped<IMailService, MailService>();

builder.Services.AddHttpClient("Mambeno")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All });
builder.Services.AddScoped<IMambenoConfigurationProvider, MambenoConfigurationProvider>();
builder.Services.AddScoped<MambenoHttpClientFactory>();
builder.Services.AddScoped<IMambenoService, MambenoService>();

builder.Services.AddScoped<IRecipeQueryService, RecipeQueryService>();
builder.Services.AddScoped<IRecipeSyncService, RecipeSyncService>();
builder.Services.AddHostedService<RecipeSyncWorker>();

builder.Services.AddHttpClient<ISmsService, SmsService>((sp, client) =>
{
    var baseUrl = sp.GetRequiredService<IConfiguration>()["Sms:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
    await dbContext.Database.MigrateAsync();
    await DataSeeder.SeedAsync(dbContext);
}

app.UseExceptionHandler();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" && HttpMethods.IsGet(context.Request.Method))
    {
        context.Response.Redirect("/swagger");
        return;
    }

    await next();
});

app.MapOpenApi().AllowAnonymous();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", apiName);
    options.DocumentTitle = apiName;
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    options.DefaultModelsExpandDepth(-1);
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
