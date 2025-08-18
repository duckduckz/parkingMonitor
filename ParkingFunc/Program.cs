static string Require(IConfiguration cfg, params string[] keys)
{
    foreach (var k in keys)
    {
        var v = cfg[k];
        if (!string.IsNullOrWhiteSpace(v)) return v!;
    }
    throw new InvalidOperationException($"Missing app setting. Tried: {string.Join(", ", keys)}");
}

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((ctx, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;

    
        var app = new AppCfg
        {
            CosmosDbName           = Require(cfg, "CosmosDbName"),
            ParkingEventsContainer = Require(cfg, "ParkingEventsContainer"),
            MaintenanceContainer   = Require(cfg, "MaintenanceContainer"),
            MaintenanceQueueName   = Require(cfg, "MaintenanceQueueName"),
            MaintenanceTable       = Require(cfg, "MaintenanceTable"),
            ExportContainer        = Require(cfg, "ExportContainer")
        };
        services.AddSingleton(app);

        var cosmosConn  = Require(cfg, "CosmosDbConn", "CosmosConn");
        var storageConn = Require(cfg, "StorageConn");

        var cosmosOptions = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };
        services.AddSingleton(new CosmosClient(cosmosConn, cosmosOptions));

        services.AddSingleton(new BlobServiceClient(storageConn));
        services.AddSingleton(_ =>
            new QueueClient(storageConn, app.MaintenanceQueueName,
                new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));
        services.AddSingleton(_ => new TableClient(storageConn, app.MaintenanceTable));

        // Repositories
        services.AddSingleton<IParkingEventRepository, CosmosParkingEventRepository>();
        services.AddSingleton<IMaintenanceRepository, CosmosMaintenanceRepository>();
        services.AddSingleton<IMaintenanceQueueRepository, StorageQueueMaintenanceRepository>();
        services.AddSingleton<IMaintenanceTableRepository, TableMaintenanceRepository>();
        services.AddSingleton<IParkingEventsExportRepository, CosmosToBlobExportRepository>();
        services.AddSingleton<IParkingEventQueryRepository, ParkingEventQueryRepository>(); 

        // Services
        services.AddSingleton<IParkingEventService, ParkingEventService>();
        services.AddSingleton<IMaintenanceService, MaintenanceService>();
        services.AddSingleton<IMaintenanceQueueService, MaintenanceQueueService>();
        services.AddSingleton<IMaintenanceTableService, MaintenanceTableService>();
        services.AddSingleton<IParkingEventsExportService, ParkingEventsExportService>();
        services.AddSingleton<IParkingEventQueryService, ParkingEventQueryService>(); 
    })
    .Build();

host.Run();
