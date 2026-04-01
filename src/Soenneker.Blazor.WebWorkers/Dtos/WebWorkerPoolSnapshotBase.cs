using System.Collections.Generic;
using Soenneker.Blazor.WebWorkers.Enums;

namespace Soenneker.Blazor.WebWorkers.Dtos;

/// <summary>
/// Common pool snapshot shape shared by both execution backends.
/// </summary>
public class WebWorkerPoolSnapshotBase<TWorkerSnapshot> where TWorkerSnapshot : WebWorkerWorkerSnapshotBase
{
    /// <summary>
    /// Logical pool name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Backend hosted by the pool.
    /// </summary>
    public WebWorkerBackend Backend { get; set; }

    /// <summary>
    /// Worker script path used when creating pool workers.
    /// </summary>
    public string ScriptPath { get; set; } = null!;

    /// <summary>
    /// Browser worker loading mode for the pool.
    /// </summary>
    public WebWorkerScriptType WorkerType { get; set; }

    /// <summary>
    /// Runtime script path used to boot the .NET runtime inside each worker when applicable.
    /// </summary>
    public string? RuntimeScriptPath { get; set; }

    /// <summary>
    /// Boot configuration path used to discover runtime assets when applicable.
    /// </summary>
    public string? BootConfigPath { get; set; }

    /// <summary>
    /// Number of workers in the pool.
    /// </summary>
    public int WorkerCount { get; set; }

    /// <summary>
    /// Number of workers currently executing requests.
    /// </summary>
    public int BusyWorkerCount { get; set; }

    /// <summary>
    /// Number of requests waiting to be assigned.
    /// </summary>
    public int QueuedCount { get; set; }

    /// <summary>
    /// Number of requests currently running.
    /// </summary>
    public int RunningCount { get; set; }

    /// <summary>
    /// Total number of completed requests for the pool.
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// Total number of cancelled requests for the pool.
    /// </summary>
    public int CancelledCount { get; set; }

    /// <summary>
    /// Total number of faulted requests for the pool.
    /// </summary>
    public int FaultedCount { get; set; }

    /// <summary>
    /// Indicates whether faulted workers are replaced automatically.
    /// </summary>
    public bool RestartFaultedWorkers { get; set; }

    /// <summary>
    /// Per-worker snapshots for the pool.
    /// </summary>
    public List<TWorkerSnapshot> Workers { get; set; } = [];
}
