using System.Text.Json.Serialization;

namespace Soenneker.Blazor.WebWorkers.Enums;

/// <summary>
/// Represents the terminal state of a web worker job.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WebWorkerJobState
{
    /// <summary>
    /// Represents the running value.
    /// </summary>
    Running,
    /// <summary>
    /// Represents the completed value.
    /// </summary>
    Completed,
    /// <summary>
    /// Represents the cancelled value.
    /// </summary>
    Cancelled,
    /// <summary>
    /// Represents the faulted value.
    /// </summary>
    Faulted
}
