namespace ParkingFunc.Repositories;

public record ParkingEventsFilter(string? ParkingName, DateTimeOffset? From, DateTimeOffset? To);

public interface IParkingEventQueryRepository
{
    IAsyncEnumerable<ParkingEventRow> QueryAsync(ParkingEventsFilter filter, CancellationToken ct = default);
}

public class ParkingEventQueryRepository : IParkingEventQueryRepository
{
    private readonly Container _container;
    public ParkingEventQueryRepository(CosmosClient client, AppCfg cfg)
        => _container = client.GetContainer(cfg.CosmosDbName, cfg.ParkingEventsContainer);

    public async IAsyncEnumerable<ParkingEventRow> QueryAsync(ParkingEventsFilter filter, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var sb = new StringBuilder("SELECT c.parkingName, c.timestamp, c.licensePlate, c.vehicleType, c.eventType, c.isMaintenance FROM c");
        var where = new List<string>();
        var qd = new QueryDefinition(""); 

        if (!string.IsNullOrWhiteSpace(filter.ParkingName))
        {
            where.Add("c.parkingName = @pn");
            qd = qd.WithParameter("@pn", filter.ParkingName);
        }
        if (filter.From is not null)
        {
            where.Add("c.timestamp >= @from");
            qd = qd.WithParameter("@from", filter.From.Value);
        }
        if (filter.To is not null)
        {
            where.Add("c.timestamp <= @to");
            qd = qd.WithParameter("@to", filter.To.Value);
        }
        if (where.Count > 0) sb.Append(" WHERE ").Append(string.Join(" AND ", where));

        qd = new QueryDefinition(sb.ToString())
            .WithParameterIfPresent("@pn", filter.ParkingName)
            .WithParameterIfPresent("@from", filter.From)
            .WithParameterIfPresent("@to", filter.To);

        var it = _container.GetItemQueryIterator<ParkingEventRow>(qd, requestOptions: new QueryRequestOptions { MaxItemCount = 1000 });
        while (it.HasMoreResults && !ct.IsCancellationRequested)
        {
            foreach (var row in await it.ReadNextAsync(ct))
                yield return row;
        }
    }
}

internal static class QueryDefinitionParamExtensions
{
    public static QueryDefinition WithParameterIfPresent(this QueryDefinition qd, string name, object? value)
        => value is null ? qd : qd.WithParameter(name, value);
}