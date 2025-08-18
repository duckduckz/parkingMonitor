namespace ParkingFunc.Functions;

public class IoTHubIngress
{
    private readonly ILogger<IoTHubIngress> _log;
    private readonly IParkingEventService _parkingSvc;
    private readonly IMaintenanceService _maintSvc;
    private readonly IMaintenanceQueueService _queueSvc;

    public IoTHubIngress(ILogger<IoTHubIngress> log,
                         IParkingEventService parkingSvc,
                         IMaintenanceService maintSvc,
                         IMaintenanceQueueService queueSvc)
    {
        _log = log;
        _parkingSvc = parkingSvc;
        _maintSvc = maintSvc;
        _queueSvc = queueSvc;
    }

    [Function(nameof(IoTHubIngress))]
    public async Task RunAsync(
        [EventHubTrigger(
            eventHubName: "iothub-ehub-parkingiot-64935085-8c1beb9efb",
            Connection = "IotHubEventHubConn",
            ConsumerGroup = "$Default")]
        EventData[] events,
        CancellationToken ct)
    {
        await _queueSvc.EnsureExistsAsync(ct);

        foreach (var e in events)
        {
            try
            {
                var json = Encoding.UTF8.GetString(e.EventBody.ToArray());
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                bool isMaintenance = root.GetProperty("isMaintenance").GetBoolean();
                string parkingName = root.GetProperty("parkingName").GetString()!;
                string eventType   = root.GetProperty("eventType").GetString()!;
                string licensePlate= root.GetProperty("licensePlate").GetString()!;
                string vehicleType = root.GetProperty("vehicleType").GetString()!;
                DateTimeOffset ts  = root.GetProperty("timestamp").GetDateTimeOffset();

                if (!isMaintenance && (eventType == "entry" || eventType == "exit"))
                {
                    var ev = new ParkingEventDoc
                    {
                        ParkingName = parkingName,
                        Timestamp = ts,
                        LicensePlate = licensePlate,
                        VehicleType = vehicleType,
                        EventType = eventType,
                        IsMaintenance = false
                    };
                    await _parkingSvc.RecordEntryExitAsync(ev, ct);
                }
                else
                {
                    var desc = root.TryGetProperty("maintenanceDescription", out var mEl) ? (mEl.GetString() ?? "unknown") : "unknown";
                    var md = new MaintenanceDoc
                    {
                        ParkingName = parkingName,
                        Timestamp = ts,
                        LicensePlate = licensePlate,
                        VehicleType = vehicleType,
                        EventType = "maintenance",
                        IsMaintenance = true,
                        MaintenanceDescription = desc
                    };
                    await _maintSvc.RecordMaintenanceAsync(md, ct);
                    await _queueSvc.EnqueueAsync(new MaintenanceQueueItem(md.Id, md.ParkingName, md.Timestamp, md.MaintenanceDescription), ct);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to process IoT Hub event: {Body}", Encoding.UTF8.GetString(e.EventBody.ToArray()));
            }
        }
    }
}
