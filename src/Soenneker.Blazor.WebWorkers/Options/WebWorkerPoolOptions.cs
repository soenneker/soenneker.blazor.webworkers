using Soenneker.Blazor.WebWorkers.Constants;
using Soenneker.Blazor.WebWorkers.Enums;

namespace Soenneker.Blazor.WebWorkers.Options;

/// <summary>
/// Defines a browser worker pool managed by the package coordinator.
/// </summary>
public sealed class WebWorkerPoolOptions
{
    private WebWorkerBackend _backend;
    private string? _name;
    private string? _scriptPath;
    private int _workerCount;
    private WebWorkerScriptType _workerType;
    private bool _nameConfigured;
    private bool _scriptPathConfigured;
    private bool _workerCountConfigured;
    private bool _workerTypeConfigured;

    public WebWorkerPoolOptions()
    {
        Backend = WebWorkerBackend.JavaScript;
    }

    /// <summary>
    /// Determines which backend the pool should host.
    /// </summary>
    public WebWorkerBackend Backend
    {
        get => _backend;
        set
        {
            _backend = value;

            if (!_nameConfigured)
                _name = value == WebWorkerBackend.DotNet ? DotNetWebWorkersDefaults.DefaultPoolName : WebWorkersDefaults.DefaultPoolName;

            if (!_scriptPathConfigured)
                _scriptPath = value == WebWorkerBackend.DotNet ? DotNetWebWorkersDefaults.DefaultWorkerScriptPath : null;

            if (!_workerCountConfigured)
                _workerCount = value == WebWorkerBackend.DotNet ? 1 : 2;

            if (!_workerTypeConfigured)
                _workerType = value == WebWorkerBackend.DotNet ? WebWorkerScriptType.Module : WebWorkerScriptType.Classic;
        }
    }

    /// <summary>
    /// Logical name used to address the pool from .NET. When omitted, the package default pool is used.
    /// </summary>
    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            _nameConfigured = true;
        }
    }

    /// <summary>
    /// Script path used when creating each worker instance.
    /// JavaScript pools require this to be set. .NET pools default to the package bootstrap worker.
    /// </summary>
    public string? ScriptPath
    {
        get => _scriptPath;
        set
        {
            _scriptPath = value;
            _scriptPathConfigured = true;
        }
    }

    /// <summary>
    /// Number of workers the pool should spin up.
    /// </summary>
    public int WorkerCount
    {
        get => _workerCount;
        set
        {
            _workerCount = value;
            _workerCountConfigured = true;
        }
    }

    /// <summary>
    /// Determines whether the browser should load the worker as a classic script or an ECMAScript module.
    /// </summary>
    public WebWorkerScriptType WorkerType
    {
        get => _workerType;
        set
        {
            _workerType = value;
            _workerTypeConfigured = true;
        }
    }

    /// <summary>
    /// Path to the browser runtime script used to boot the .NET worker.
    /// When omitted, the coordinator resolves <c>_framework/dotnet.js</c> from the current document base URI.
    /// </summary>
    public string? RuntimeScriptPath { get; set; }

    /// <summary>
    /// Path to the Blazor boot configuration used to discover the main application assembly and runtime assets.
    /// When omitted, the coordinator resolves <c>_framework/blazor.boot.json</c> from the current document base URI.
    /// </summary>
    public string? BootConfigPath { get; set; }

    /// <summary>
    /// When true, failed workers are replaced automatically after a terminal fault.
    /// </summary>
    public bool RestartFaultedWorkers { get; set; } = true;
}
