namespace ParkingFunc.Functions;

public class ExportParkingEvents
{
    private readonly ILogger<ExportParkingEvents> _log;
    private readonly IParkingEventsExportService _exportSvc;

    public ExportParkingEvents(ILogger<ExportParkingEvents> log, IParkingEventsExportService exportSvc)
    {
        _log = log;
        _exportSvc = exportSvc;
    }

    [Function(nameof(ExportParkingEvents))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "export/parkingevents")] HttpRequestData req,
        CancellationToken ct)
    {
        try
        {
            var qs = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string? pn = qs["parkingName"];
            DateTimeOffset? from = DateTimeOffset.TryParse(qs["from"], out var f) ? f : null;
            DateTimeOffset? to   = DateTimeOffset.TryParse(qs["to"], out var t) ? t : null;

            var result = await _exportSvc.ExportAsync(pn, from, to, ct);

            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(new
            {
                downloadUrl = result.DownloadUrl.ToString(),
                blobName = result.BlobName,
                container = "exports",
                rows = result.Rows
            });
            return res;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Export failed.");
            var res = req.CreateResponse(HttpStatusCode.InternalServerError);
            await res.WriteStringAsync("Export failed: " + ex.Message);
            return res;
        }
    }
}
