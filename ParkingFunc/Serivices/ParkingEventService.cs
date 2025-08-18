namespace ParkingFunc.Services;

public interface IParkingEventService
{
    Task RecordEntryExitAsync(ParkingEventDoc doc, CancellationToken ct = default);
}

public class ParkingEventService : IParkingEventService
{
    private readonly IParkingEventRepository _repo;
    public ParkingEventService(IParkingEventRepository repo) => _repo = repo;

    public Task RecordEntryExitAsync(ParkingEventDoc doc, CancellationToken ct = default)
        => _repo.UpsertAsync(doc, ct);
}
