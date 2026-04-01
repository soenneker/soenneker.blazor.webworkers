namespace Soenneker.Blazor.WebWorkers.Constants;

/// <summary>
/// Known defaults for the .NET worker coordinator.
/// </summary>
public static class DotNetWebWorkersDefaults
{
    /// <summary>
    /// Package-managed default pool name used when no explicit pool name is supplied.
    /// </summary>
    public const string DefaultPoolName = "default";

    /// <summary>
    /// Package-managed generic worker bootstrap script for .NET WebAssembly workers.
    /// </summary>
    public const string DefaultWorkerScriptPath = WebWorkerAssetPaths.DotNetWorkerScript;
}
