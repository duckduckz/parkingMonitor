namespace ParkingFunc.Services;

public interface IMaintenanceQueueService
{
    Task EnsureExistsAsync(CancellationToken ct = default);
    Task EnqueueAsync(MaintenanceQueueItem item, CancellationToken ct = default);
}


public class MaintenanceQueueService : IMaintenanceQueueService
{
    private readonly IMaintenanceQueueRepository _repo;
    public MaintenanceQueueService(IMaintenanceQueueRepository repo) => _repo = repo;

    public Task EnsureExistsAsync(CancellationToken ct = default) => _repo.EnsureExistsAsync(ct);
    public Task EnqueueAsync(MaintenanceQueueItem item, CancellationToken ct = default) => _repo.EnqueueAsync(item, ct);
}
