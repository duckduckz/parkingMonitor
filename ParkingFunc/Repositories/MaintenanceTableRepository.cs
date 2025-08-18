namespace ParkingFunc.Repositories;

public interface IMaintenanceTableRepository
{
    Task UpsertAsync(string parkingName, string id, string maintenanceType, string assignedTech, DateTimeOffset eventTime, CancellationToken ct = default);
}


public class TableMaintenanceRepository : IMaintenanceTableRepository
{
    private readonly TableClient _table;

    private class Entity : ITableEntity
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

    public TableMaintenanceRepository(TableClient table) => _table = table;

    public async Task UpsertAsync(string parkingName, string id, string maintenanceType, string assignedTech, DateTimeOffset eventTime, CancellationToken ct = default)
    {
        await _table.CreateIfNotExistsAsync(ct);
        var entity = new Entity
        {
            PartitionKey = parkingName,
            RowKey = id,
            ParkingName = parkingName,
            MaintenanceType = maintenanceType,
            AssignedTechnician = assignedTech,
            EventTime = eventTime
        };
        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
    }
}
