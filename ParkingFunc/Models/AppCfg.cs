namespace ParkingFunc.Models;

public class AppCfg
{
    public string CosmosDbName { get; init; } = default!;
    public string ParkingEventsContainer { get; init; } = default!;
    public string MaintenanceContainer { get; init; } = default!;
    public string MaintenanceQueueName { get; init; } = default!;
    public string MaintenanceTable { get; init; } = default!;   
    public string ExportContainer { get; init; } = default!;     
}

