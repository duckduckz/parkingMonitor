namespace ParkingFunc.Models;

public class MaintenanceEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;  
    public string RowKey { get; set; } = default!;        
    public string ParkingName { get; set; } = default!;
    public string MaintenanceType { get; set; } = default!;
    public string AssignedTechnician { get; set; } = default!;
    public DateTimeOffset EventTime { get; set; }
    public ETag ETag { get; set; } = ETag.All;
    public DateTimeOffset? Timestamp { get; set; }        
}