using System.Collections.Generic;
using Soenneker.Blazor.WebWorkers.Enums;

namespace Soenneker.Blazor.WebWorkers.Dtos;

/// <summary>
/// Common coordinator snapshot shape shared by both execution backends.
/// </summary>
public class WebWorkerCoordinatorSnapshotBase<TPoolSnapshot>
{
    /// <summary>
    /// Backend represented by the snapshot.
    /// </summary>
    public WebWorkerBackend Backend { get; set; }

    /// <summary>
    /// Number of pools currently registered.
    /// </summary>
    public int PoolCount { get; set; }

    /// <summary>
    /// Total number of workers across all pools.
    /// </summary>
    public int TotalWorkers { get; set; }

    /// <summary>
    /// Number of workers currently busy.
    /// </summary>
    public int BusyWorkers { get; set; }

    /// <summary>
    /// Number of requests queued across all pools.
    /// </summary>
    public int QueuedCount { get; set; }

    /// <summary>
    /// Number of requests running across all pools.
    /// </summary>
    public int RunningCount { get; set; }

    /// <summary>
    /// Total number of completed requests observed by the coordinator.
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// Total number of cancelled requests observed by the coordinator.
    /// </summary>
    public int CancelledCount { get; set; }

    /// <summary>
    /// Total number of faulted requests observed by the coordinator.
    /// </summary>
    public int FaultedCount { get; set; }

    /// <summary>
    /// Snapshots for each registered pool.
    /// </summary>
    public List<TPoolSnapshot> Pools { get; set; } = [];
}
