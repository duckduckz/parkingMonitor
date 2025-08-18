namespace ParkingFunc.Services;


public interface IMaintenanceService
{
    Task RecordMaintenanceAsync(MaintenanceDoc doc, CancellationToken ct = default);
}

public class MaintenanceService : IMaintenanceService
{
    private readonly IMaintenanceRepository _repo;
    public MaintenanceService(IMaintenanceRepository repo) => _repo = repo;

    public Task RecordMaintenanceAsync(MaintenanceDoc doc, CancellationToken ct = default)
        => _repo.UpsertAsync(doc, ct);
}
