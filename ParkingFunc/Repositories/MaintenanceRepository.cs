namespace ParkingFunc.Repositories;

public interface IMaintenanceRepository
{
    Task UpsertAsync(MaintenanceDoc doc, CancellationToken ct = default);
}

public class CosmosMaintenanceRepository : IMaintenanceRepository
{
    private readonly Container _container;
    private readonly string _pkPath; // lowercase

    public CosmosMaintenanceRepository(CosmosClient client, AppCfg cfg)
    {
        _container = client.GetContainer(cfg.CosmosDbName, cfg.MaintenanceContainer);

        var path = _container.ReadContainerAsync().GetAwaiter().GetResult().Resource.PartitionKeyPath;
        _pkPath = string.IsNullOrWhiteSpace(path) ? "/id" : path.Trim().ToLowerInvariant();
    }

    private PartitionKey BuildPk(MaintenanceDoc doc) => _pkPath switch
    {
        "/parkingname" => !string.IsNullOrWhiteSpace(doc.ParkingName)
            ? new PartitionKey(doc.ParkingName)
            : throw new InvalidOperationException("parkingName (partition key) is required but was null/empty."),

        "/id" => new PartitionKey(doc.Id),

        "/eventtype" => !string.IsNullOrWhiteSpace(doc.EventType)
            ? new PartitionKey(doc.EventType)
            : throw new InvalidOperationException("eventType (partition key) is required but was null/empty."),

        _ => throw new InvalidOperationException($"Unsupported partition key path '{_pkPath}'.")
    };

    public Task UpsertAsync(MaintenanceDoc doc, CancellationToken ct = default)
        => _container.UpsertItemAsync(doc, BuildPk(doc), cancellationToken: ct);
}
