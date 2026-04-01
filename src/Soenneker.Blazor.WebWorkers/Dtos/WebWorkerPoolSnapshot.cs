using Soenneker.Blazor.WebWorkers.Enums;

namespace Soenneker.Blazor.WebWorkers.Dtos;

/// <summary>
/// Snapshot of a worker pool managed by the coordinator.
/// </summary>
public sealed class WebWorkerPoolSnapshot : WebWorkerPoolSnapshotBase<WebWorkerWorkerSnapshot>
{
    public WebWorkerPoolSnapshot()
    {
        Backend = WebWorkerBackend.JavaScript;
    }
}
