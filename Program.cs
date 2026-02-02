using hikvision_integration.Configuration;
using hikvision_integration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DotNetEnv;

// Use exe directory as content root (important when running as Windows Service)
var contentRoot = AppContext.BaseDirectory;
if (File.Exists(Path.Combine(contentRoot, ".env")))
{
    Env.Load(Path.Combine(contentRoot, ".env"));
}
else if (File.Exists(".env"))
{
    Env.Load();
}

var host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(contentRoot)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Hikvision Sync Service";
    })
    .ConfigureAppConfiguration((context, config) =>
    {
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
        services.AddHostedService<SyncWorker>();
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
logger.LogInformation("Hikvision Sync Service starting");

await host.RunAsync();

logger.LogInformation("Hikvision Sync Service stopped");
