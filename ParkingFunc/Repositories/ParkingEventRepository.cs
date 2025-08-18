namespace ParkingFunc.Repositories;

public interface IParkingEventRepository
{
    Task UpsertAsync(ParkingEventDoc doc, CancellationToken ct = default);
}

public class CosmosParkingEventRepository : IParkingEventRepository
{
    private readonly Container _container;
    private readonly string _pkPath; // always lowercase, leading slash

    public CosmosParkingEventRepository(CosmosClient client, AppCfg cfg)
    {
        _container = client.GetContainer(cfg.CosmosDbName, cfg.ParkingEventsContainer);

        var path = _container.ReadContainerAsync().GetAwaiter().GetResult().Resource.PartitionKeyPath;
        _pkPath = string.IsNullOrWhiteSpace(path) ? "/id" : path.Trim().ToLowerInvariant();
    }

    private PartitionKey BuildPk(ParkingEventDoc doc) => _pkPath switch
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

    public Task UpsertAsync(ParkingEventDoc doc, CancellationToken ct = default)
        => _container.UpsertItemAsync(doc, BuildPk(doc), cancellationToken: ct);
}
