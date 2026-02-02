using hikvision_integration.Configuration;
using hikvision_integration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DotNetEnv;

// Load .env file if it exists
Env.Load();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // Configuration priority: appsettings.json -> appsettings.{Environment}.json -> Environment Variables
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;
        
        // Bind and validate configuration
        var syncOptions = new SyncOptions();
        config.GetSection(SyncOptions.SectionName).Bind(syncOptions);
        
        // Validate required configuration
        ValidateConfiguration(syncOptions);
        
        services.Configure<SyncOptions>(config.GetSection(SyncOptions.SectionName));

        services.AddHttpClient<IGymApiService, GymApiService>();

        services.AddSingleton<ISyncStateStore, FileSyncStateStore>();
        services.AddSingleton<IHikvisionApiService, HikvisionApiService>();
        services.AddSingleton<ISyncService, SyncService>();
    })
    .Build();

static void ValidateConfiguration(SyncOptions options)
{
    var errors = new List<string>();
    
    if (string.IsNullOrWhiteSpace(options.Gym.ApiUrl))
        errors.Add("Sync:Gym:ApiUrl is required");
    
    if (string.IsNullOrWhiteSpace(options.Hikvision.Username))
        errors.Add("Sync:Hikvision:Username is required");
    
    if (string.IsNullOrWhiteSpace(options.Hikvision.Password))
        errors.Add("Sync:Hikvision:Password is required");
    
    if (string.IsNullOrWhiteSpace(options.Hikvision.BaseUrl) && options.Hikvision.ReaderUrls.Count == 0)
        errors.Add("Either Sync:Hikvision:BaseUrl or Sync:Hikvision:ReaderUrls must be configured");
    
    if (errors.Any())
    {
        Console.Error.WriteLine("Configuration validation failed:");
        foreach (var error in errors)
            Console.Error.WriteLine($"  - {error}");
        Environment.Exit(1);
    }
}

var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    var syncService = host.Services.GetRequiredService<ISyncService>();
    var result = await syncService.RunSyncAsync();

    if (logger.IsEnabled(LogLevel.Information))
    {
        logger.LogInformation(
            "Sync finished: {Total} total, {Synced} synced, {Skipped} skipped, {Failed} failed",
            result.TotalMembers, result.Synced, result.Skipped, result.Failed);
    }

    if (result.Errors.Count > 0 && logger.IsEnabled(LogLevel.Warning))
    {
        foreach (var err in result.Errors)
            logger.LogWarning("Error: {Error}", err);
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Sync failed");
    Environment.Exit(1);
}
