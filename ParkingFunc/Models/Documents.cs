namespace ParkingFunc.Models;

public abstract class BaseDoc
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("parkingName")]
    public string ParkingName { get; set; } = default!;

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("licensePlate")]
    public string LicensePlate { get; set; } = default!;

    [JsonPropertyName("vehicleType")]
    public string VehicleType { get; set; } = default!;

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = default!; 

    [JsonPropertyName("isMaintenance")]
    public bool IsMaintenance { get; set; }
}

public class ParkingEventDoc : BaseDoc
{

}

public class MaintenanceDoc : BaseDoc
{
    [JsonPropertyName("maintenanceDescription")]
    public string MaintenanceDescription { get; set; } = default!;
}

public record MaintenanceQueueItem(
    string Id,
    string ParkingName,
    DateTimeOffset Timestamp,
    string MaintenanceDescription
);


public class ParkingEventRow
{
    public string? ParkingName { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? LicensePlate { get; set; }
    public string? VehicleType { get; set; }
    public string? EventType { get; set; }
    public bool IsMaintenance { get; set; }
}

public record ExportResult(string BlobName, Uri DownloadUrl, long Rows);