namespace ParkingFunc.Repositories;

public interface IMaintenanceQueueRepository
{
    Task EnsureExistsAsync(CancellationToken ct = default);
    Task EnqueueAsync(MaintenanceQueueItem item, CancellationToken ct = default);
}

public class StorageQueueMaintenanceRepository : IMaintenanceQueueRepository
{
    private readonly QueueClient _queue;
    private static readonly JsonSerializerOptions _opts = new(JsonSerializerDefaults.Web);

    public StorageQueueMaintenanceRepository(QueueClient queue) => _queue = queue;

    public Task EnsureExistsAsync(CancellationToken ct = default)
        => _queue.CreateIfNotExistsAsync(cancellationToken: ct);

    public Task EnqueueAsync(MaintenanceQueueItem item, CancellationToken ct = default)
    {
        
        var json = JsonSerializer.Serialize(item, _opts);
        return _queue.SendMessageAsync(json, cancellationToken: ct);
    }
}
