using hikvision_integration.Configuration;
using hikvision_integration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;
        services.Configure<SyncOptions>(config.GetSection(SyncOptions.SectionName));

        services.AddHttpClient<IGymApiService, GymApiService>();

        services.AddSingleton<ISyncStateStore, FileSyncStateStore>();
        services.AddSingleton<IHikvisionApiService, HikvisionApiService>();
        services.AddSingleton<ISyncService, SyncService>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    var syncService = host.Services.GetRequiredService<ISyncService>();
    var result = await syncService.RunSyncAsync();

    logger.LogInformation(
        "Sync finished: {Total} total, {Synced} synced, {Skipped} skipped, {Failed} failed",
        result.TotalMembers, result.Synced, result.Skipped, result.Failed);

    if (result.Errors.Count > 0)
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
