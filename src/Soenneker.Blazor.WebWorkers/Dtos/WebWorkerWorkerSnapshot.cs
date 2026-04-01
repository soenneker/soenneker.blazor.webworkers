using Soenneker.Blazor.WebWorkers.Enums;

namespace Soenneker.Blazor.WebWorkers.Dtos;

/// <summary>
/// Snapshot of an individual worker within a pool.
/// </summary>
public sealed class WebWorkerWorkerSnapshot : WebWorkerWorkerSnapshotBase
{
    public WebWorkerWorkerSnapshot()
    {
        Backend = WebWorkerBackend.JavaScript;
        IsReady = true;
    }
}
