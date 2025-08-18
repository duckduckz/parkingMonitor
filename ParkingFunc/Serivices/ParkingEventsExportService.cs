namespace ParkingFunc.Services;

public interface IParkingEventsExportService
{
    Task<ExportResult> ExportAsync(string? parkingName, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);
}

public class ParkingEventsExportService : IParkingEventsExportService
{
    private readonly IParkingEventsExportRepository _repo;
    public ParkingEventsExportService(IParkingEventsExportRepository repo) => _repo = repo;

    public Task<ExportResult> ExportAsync(string? parkingName, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
        => _repo.ExportToCsvAsync(parkingName, from, to, ct);
}
