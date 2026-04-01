using Soenneker.Blazor.WebWorkers.Enums;

namespace Soenneker.Blazor.WebWorkers.Dtos;

/// <summary>
/// General request shape used by both JavaScript-worker and .NET-worker execution paths.
/// </summary>
public class WebWorkerExecutionRequest
{
    /// <summary>
    /// Logical pool name that should execute the request. When omitted, the package default pool is used.
    /// </summary>
    public string? PoolName { get; set; }

    /// <summary>
    /// Backend that should process the request.
    /// </summary>
    public WebWorkerBackend Backend { get; set; } = WebWorkerBackend.JavaScript;

    /// <summary>
    /// Optional client-supplied request identifier. If omitted, the library generates one.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Logical workload name understood by a JavaScript worker script.
    /// </summary>
    public string? WorkloadName { get; set; }

    /// <summary>
    /// Fully qualified exported method name understood by the .NET worker runtime.
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// Optional payload sent to a JavaScript worker.
    /// </summary>
    public object? Payload { get; set; }

    /// <summary>
    /// Ordered argument list supplied to an exported .NET worker method.
    /// </summary>
    public object[] Arguments { get; set; } = [];

    /// <summary>
    /// Optional timeout enforced by the coordinator.
    /// </summary>
    public int? TimeoutMs { get; set; }
}
