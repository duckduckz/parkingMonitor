namespace ParkingFunc.Repositories;

// Interface
public interface IParkingEventsExportRepository
{
    Task<ExportResult> ExportToCsvAsync(string? parkingName, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);
}

public class CosmosToBlobExportRepository : IParkingEventsExportRepository
{
    private readonly CosmosClient _cosmos;
    private readonly BlobServiceClient _blob;
    private readonly AppCfg _cfg;

    public CosmosToBlobExportRepository(CosmosClient cosmos, BlobServiceClient blob, AppCfg cfg)
    {
        _cosmos = cosmos;
        _blob = blob;
        _cfg = cfg;
    }

    private static string CsvEscape(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var needsQuote = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
        s = s.Replace("\"", "\"\"");
        return needsQuote ? $"\"{s}\"" : s;
    }

    public async Task<ExportResult> ExportToCsvAsync(string? parkingName, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        var container = _cosmos.GetContainer(_cfg.CosmosDbName, _cfg.ParkingEventsContainer);

        // Build query
        var sb = new StringBuilder("SELECT c.parkingName, c.timestamp, c.licensePlate, c.vehicleType, c.eventType, c.isMaintenance FROM c");
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(parkingName)) filters.Add("c.parkingName = @pn");
        if (from.HasValue) filters.Add("c.timestamp >= @from");
        if (to.HasValue)   filters.Add("c.timestamp <= @to");
        if (filters.Count > 0) sb.Append(" WHERE ").Append(string.Join(" AND ", filters));

        var qd = new QueryDefinition(sb.ToString());
        if (!string.IsNullOrWhiteSpace(parkingName)) qd = qd.WithParameter("@pn", parkingName);
        if (from.HasValue) qd = qd.WithParameter("@from", from.Value);
        if (to.HasValue)   qd = qd.WithParameter("@to", to.Value);

        // CSV in-memory
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, new UTF8Encoding(false));
        await writer.WriteLineAsync("parkingName,timestamp,licensePlate,vehicleType,eventType,isMaintenance");

        long rows = 0;
        var it = container.GetItemQueryIterator<ParkingEventRow>(qd, requestOptions: new QueryRequestOptions { MaxItemCount = 1000 });
        while (it.HasMoreResults && !ct.IsCancellationRequested)
        {
            var page = await it.ReadNextAsync(ct);
            foreach (var r in page)
            {
                await writer.WriteLineAsync(
                    $"{CsvEscape(r.ParkingName)}," +
                    $"{CsvEscape(r.Timestamp.ToString("o"))}," +
                    $"{CsvEscape(r.LicensePlate)}," +
                    $"{CsvEscape(r.VehicleType)}," +
                    $"{CsvEscape(r.EventType)}," +
                    $"{(r.IsMaintenance ? "true" : "false")}"
                );
                rows++;
            }
        }

        await writer.FlushAsync();
        ms.Position = 0;

        var containerClient = _blob.GetBlobContainerClient(_cfg.ExportContainer);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var blobName = $"parkingevents-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        var blob = containerClient.GetBlobClient(blobName);
        await blob.UploadAsync(ms, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "text/csv" } }, ct);

        if (!blob.CanGenerateSasUri) throw new InvalidOperationException("Cannot generate SAS with current credentials.");
        var sasUri = blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(60));

        return new ExportResult(blobName, sasUri, rows);
    }
}
