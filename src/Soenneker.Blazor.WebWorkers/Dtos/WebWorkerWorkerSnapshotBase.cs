using System;
using Soenneker.Blazor.WebWorkers.Enums;

namespace Soenneker.Blazor.WebWorkers.Dtos;

/// <summary>
/// Common worker snapshot shape shared by both execution backends.
/// </summary>
public class WebWorkerWorkerSnapshotBase
{
    /// <summary>
    /// Stable worker identifier within the pool.
    /// </summary>
    public string WorkerId { get; set; } = null!;

    /// <summary>
    /// Backend hosted by this worker.
    /// </summary>
    public WebWorkerBackend Backend { get; set; }

    /// <summary>
    /// Indicates whether the worker is currently executing a request.
    /// </summary>
    public bool IsBusy { get; set; }

    /// <summary>
    /// Indicates whether the worker runtime is initialized and ready to accept work.
    /// </summary>
    public bool IsReady { get; set; } = true;

    /// <summary>
    /// Active request identifier when the worker is busy.
    /// </summary>
    public string? ActiveRequestId { get; set; }

    /// <summary>
    /// Active workload or method name when the worker is busy.
    /// </summary>
    public string? ActiveName { get; set; }

    /// <summary>
    /// Current request state when the worker is busy.
    /// </summary>
    public WebWorkerJobState? ActiveState { get; set; }

    /// <summary>
    /// Start time of the current request when one is active.
    /// </summary>
    public DateTimeOffset? StartedAtUtc { get; set; }

    /// <summary>
    /// Completion time of the last finished request.
    /// </summary>
    public DateTimeOffset? LastCompletedAtUtc { get; set; }

    /// <summary>
    /// Duration of the last finished request in milliseconds.
    /// </summary>
    public double? LastDurationMs { get; set; }
}
