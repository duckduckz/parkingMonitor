namespace ParkingFunc.Services;

public interface IParkingEventQueryService
{
    IAsyncEnumerable<ParkingEventRow> QueryAsync(ParkingEventsFilter filter, CancellationToken ct = default);
}

public class ParkingEventQueryService : IParkingEventQueryService
{
    private readonly IParkingEventQueryRepository _repo;
    public ParkingEventQueryService(IParkingEventQueryRepository repo) => _repo = repo;

    public IAsyncEnumerable<ParkingEventRow> QueryAsync(ParkingEventsFilter filter, CancellationToken ct = default)
        => _repo.QueryAsync(filter, ct);
}