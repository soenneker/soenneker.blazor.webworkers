using System;
using Soenneker.Extensions.String;

namespace Soenneker.Blazor.WebWorkers.Constants;

/// <summary>
/// Well-known static web asset paths used by the package.
/// </summary>
public static class WebWorkerAssetPaths
{
    /// <summary>
    /// Library-relative module root used with <c>IResourceLoader.ImportModule()</c>.
    /// </summary>
    public const string PackageModuleRoot = "Soenneker.Blazor.WebWorkers";

    /// <summary>
    /// Root static web asset path for this package.
    /// </summary>
    public const string PackageRoot = "_content/Soenneker.Blazor.WebWorkers";

    /// <summary>
    /// Library-relative path to the package JavaScript interop module.
    /// </summary>
    public const string InteropScript = $"{PackageModuleRoot}/js/webworkersinterop.js";

    /// <summary>
    /// Path to the package-owned generic .NET worker bootstrap script.
    /// </summary>
    public const string DotNetWorkerScript = $"{PackageRoot}/js/workers/dotnetwebworker.js";

    /// <summary>
    /// Builds a static web asset path for another Razor class library package.
    /// </summary>
    public static string FromPackage(string packageId, string relativeAssetPath)
    {
        if (packageId.IsNullOrWhiteSpace())
            throw new ArgumentException("Package id cannot be null or whitespace.", nameof(packageId));

        if (relativeAssetPath.IsNullOrWhiteSpace())
            throw new ArgumentException("Relative asset path cannot be null or whitespace.", nameof(relativeAssetPath));

        string normalized = relativeAssetPath.Trim().Replace('\\', '/').TrimStart('/');

        return $"_content/{packageId}/{normalized}";
    }

    /// <summary>
    /// Builds a conventional worker asset path under <c>js/workers</c> for another package.
    /// </summary>
    public static string WorkerFromPackage(string packageId, string workerFileName)
    {
        if (workerFileName.IsNullOrWhiteSpace())
            throw new ArgumentException("Worker file name cannot be null or whitespace.", nameof(workerFileName));

        string normalized = workerFileName.Trim().Replace('\\', '/').TrimStart('/');

        return FromPackage(packageId, $"js/workers/{normalized}");
    }
}
