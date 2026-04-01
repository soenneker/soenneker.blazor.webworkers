using Soenneker.Blazor.WebWorkers.Dtos;
using Soenneker.Blazor.WebWorkers.Enums;

namespace Soenneker.Blazor.WebWorkers.Demo.Models;

public sealed class DemoJobRecord
{
    public string JobId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public WebWorkerRequest Request { get; set; } = null!;
    public WebWorkerJobState State { get; set; }
    public double Percent { get; set; }
    public double? DurationMs { get; set; }
    public string ProgressMessage { get; set; } = null!;
    public string? ResultPreview { get; set; }
    public bool IsTerminal => State is WebWorkerJobState.Completed or WebWorkerJobState.Cancelled or WebWorkerJobState.Faulted;
}
