using System;
using Soenneker.Blazor.WebWorkers.Enums;

namespace Soenneker.Blazor.WebWorkers.Dtos;

/// <summary>
/// General terminal result shape used by both JavaScript-worker and .NET-worker execution paths.
/// </summary>
public class WebWorkerExecutionResult<TResult>
{
    /// <summary>
    /// Pool that executed the request.
    /// </summary>
    public string PoolName { get; set; } = null!;

    /// <summary>
    /// Backend that produced the result.
    /// </summary>
    public WebWorkerBackend Backend { get; set; }

    /// <summary>
    /// Unique identifier for the completed request.
    /// </summary>
    public string RequestId { get; set; } = null!;

    /// <summary>
    /// Logical workload name when the JavaScript worker path was used.
    /// </summary>
    public string? WorkloadName { get; set; }

    /// <summary>
    /// Fully qualified exported method name when the .NET worker path was used.
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// Worker that processed the request when available.
    /// </summary>
    public string? WorkerId { get; set; }

    /// <summary>
    /// Final state reported by the coordinator.
    /// </summary>
    public WebWorkerJobState State { get; set; }

    /// <summary>
    /// Typed result payload returned by the worker for successful requests.
    /// </summary>
    public TResult? Result { get; set; }

    /// <summary>
    /// Error or cancellation detail for non-successful requests.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Total request duration as measured by the coordinator.
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Timestamp for when the request reached a terminal state.
    /// </summary>
    public DateTimeOffset CompletedAtUtc { get; set; }
}
