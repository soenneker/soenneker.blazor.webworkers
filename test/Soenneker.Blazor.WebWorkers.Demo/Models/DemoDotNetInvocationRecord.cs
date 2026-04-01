using Soenneker.Blazor.WebWorkers.Enums;

namespace Soenneker.Blazor.WebWorkers.Demo.Models;

public sealed class DemoDotNetInvocationRecord
{
    public string InvocationId { get; set; } = null!;
    public string MethodName { get; set; } = null!;
    public WebWorkerJobState State { get; set; }
    public double? DurationMs { get; set; }
    public string StatusMessage { get; set; } = null!;
    public string? ResultPreview { get; set; }
    public bool IsTerminal => State is WebWorkerJobState.Completed or WebWorkerJobState.Cancelled or WebWorkerJobState.Faulted;
}
