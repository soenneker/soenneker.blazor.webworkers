using System.Text.Json.Serialization;

namespace Soenneker.Blazor.WebWorkers.Enums;

/// <summary>
/// Represents the terminal state of a web worker job.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WebWorkerJobState
{
    Running,
    Completed,
    Cancelled,
    Faulted
}
