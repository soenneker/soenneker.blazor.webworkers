namespace Soenneker.Blazor.WebWorkers.Dtos;

/// <summary>
/// Snapshot of all pools currently managed by the coordinator.
/// </summary>
public sealed class WebWorkerCoordinatorSnapshot : WebWorkerCoordinatorSnapshotBase<WebWorkerPoolSnapshot>
{
    public WebWorkerCoordinatorSnapshot()
    {
        Backend = Enums.WebWorkerBackend.JavaScript;
    }
}
