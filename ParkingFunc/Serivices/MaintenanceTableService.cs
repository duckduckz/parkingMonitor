namespace ParkingFunc.Services;

// Interface
public interface IMaintenanceTableService
{
    Task AssignAndSaveAsync(string id, string parkingName, string maintenanceDescription, DateTimeOffset timestamp, CancellationToken ct = default);
}

public class MaintenanceTableService : IMaintenanceTableService
{
    private readonly IMaintenanceTableRepository _repo;
    public MaintenanceTableService(IMaintenanceTableRepository repo) => _repo = repo;

    private static string Assign(string desc)
    {
        var k = (desc ?? "").Trim().ToLowerInvariant().Replace("gare","gate").Replace("requried","required");
        return k switch
        {
            "gate malfunction" => "Tom Smit",
            "payment machine broken" => "Sara De Vos",
            "cleaning required" => "Nancy De Backer",
            "lighting broken" => "Arno Peeters",
            _ => "Unassigned"
        };
    }

    public Task AssignAndSaveAsync(string id, string parkingName, string maintenanceDescription, DateTimeOffset timestamp, CancellationToken ct = default)
    {
        var tech = Assign(maintenanceDescription);
        return _repo.UpsertAsync(parkingName, id, maintenanceDescription, tech, timestamp, ct);
    }
}
