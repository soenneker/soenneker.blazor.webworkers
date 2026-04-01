using System.Text.Json.Serialization;

namespace Soenneker.Blazor.WebWorkers.Enums;

/// <summary>
/// Identifies how the browser should load a worker script.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WebWorkerScriptType
{
    /// <summary>
    /// Uses the browser's classic worker mode.
    /// </summary>
    Classic = 0,

    /// <summary>
    /// Uses the browser's ECMAScript module worker mode.
    /// </summary>
    Module = 1
}
