using System;

namespace Soenneker.Blazor.WebWorkers.Dtos;

/// <summary>
/// Progress update emitted by the worker coordinator.
/// </summary>
public sealed class WebWorkerJobProgress
{
    /// <summary>
    /// Pool currently executing the job.
    /// </summary>
    public string PoolName { get; set; } = null!;

    /// <summary>
    /// Unique identifier for the job being reported.
    /// </summary>
    public string JobId { get; set; } = null!;

    /// <summary>
    /// Logical workload name being executed.
    /// </summary>
    public string WorkloadName { get; set; } = null!;

    /// <summary>
    /// Worker instance currently processing the job.
    /// </summary>
    public string WorkerId { get; set; } = null!;

    /// <summary>
    /// Percent complete from 0 to 100.
    /// </summary>
    public double Percent { get; set; }

    /// <summary>
    /// Number of completed units reported by the workload.
    /// </summary>
    public int CompletedUnits { get; set; }

    /// <summary>
    /// Total unit count reported by the workload when known.
    /// </summary>
    public int TotalUnits { get; set; }

    /// <summary>
    /// Optional coarse-grained stage name from the worker.
    /// </summary>
    public string? Stage { get; set; }

    /// <summary>
    /// Optional human-readable progress message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Timestamp for when the progress event was emitted.
    /// </summary>
    public DateTimeOffset TimestampUtc { get; set; }
}
