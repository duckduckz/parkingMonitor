namespace ParkingFunc.Functions;

public class ProcessMaintenanceQueue
{
    private readonly ILogger<ProcessMaintenanceQueue> _log;
    private readonly IMaintenanceTableService _tableSvc;

    public ProcessMaintenanceQueue(ILogger<ProcessMaintenanceQueue> log, IMaintenanceTableService tableSvc)
    {
        _log = log;
        _tableSvc = tableSvc;
    }

    [Function(nameof(ProcessMaintenanceQueue))]
    public async Task RunAsync(
        [QueueTrigger("%MaintenanceQueueName%", Connection = "StorageConn")] string message,
        CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            string id   = root.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
            string pn   = root.GetProperty("parkingName").GetString() ?? "unknown";
            string desc = root.GetProperty("maintenanceDescription").GetString() ?? "unknown";
            var ts      = root.TryGetProperty("timestamp", out var tEl) ? tEl.GetDateTimeOffset() : DateTimeOffset.UtcNow;

            await _tableSvc.AssignAndSaveAsync(id, pn, desc, ts, ct);
            _log.LogInformation("Saved maintenance assignment for {Id} at {Pn}", id, pn);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed processing maintenance queue message: {Message}", message);
            throw; 
        }
    }
}
