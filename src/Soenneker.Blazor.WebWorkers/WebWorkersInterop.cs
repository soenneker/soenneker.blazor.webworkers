using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Soenneker.Asyncs.Initializers;
using Soenneker.Blazor.Utils.ResourceLoader.Abstract;
using Soenneker.Blazor.WebWorkers.Abstract;
using Soenneker.Blazor.WebWorkers.Constants;
using Soenneker.Blazor.WebWorkers.Dtos;
using Soenneker.Blazor.WebWorkers.Dtos.Abstract;
using Soenneker.Blazor.WebWorkers.Enums;
using Soenneker.Blazor.WebWorkers.Options;
using Soenneker.Extensions.CancellationTokens;
using Soenneker.Utils.CancellationScopes;
using Soenneker.Utils.Json;

namespace Soenneker.Blazor.WebWorkers;

/// <inheritdoc cref="IWebWorkersInterop"/>
public sealed class WebWorkersInterop : IWebWorkersInterop
{
    private const string _modulePath = WebWorkerAssetPaths.InteropScript;
    private const string _jsInitialize = "WebWorkersInterop.initialize";
    private const string _jsCreatePool = "WebWorkersInterop.createPool";
    private const string _jsPoolExists = "WebWorkersInterop.poolExists";
    private const string _jsRunRequest = "WebWorkersInterop.runRequest";
    private const string _jsCancelRequest = "WebWorkersInterop.cancelRequest";
    private const string _jsDestroyPool = "WebWorkersInterop.destroyPool";
    private const string _jsGetPoolSnapshot = "WebWorkersInterop.getPoolSnapshot";
    private const string _jsGetPoolSnapshots = "WebWorkersInterop.getPoolSnapshots";
    private const string _jsGetCoordinatorSnapshot = "WebWorkersInterop.getCoordinatorSnapshot";
    private const string _jsDispose = "WebWorkersInterop.dispose";

    private readonly IJSRuntime _jsRuntime;
    private readonly IResourceLoader _resourceLoader;
    private readonly AsyncInitializer _moduleInitializer;
    private readonly AsyncInitializer _jsInitializer;
    private readonly CancellationScope _cancellationScope = new();
    private readonly ConcurrentDictionary<string, IPendingJob> _pendingJobs = new();
    private readonly ConcurrentDictionary<string, IDotNetPendingInvocation> _pendingInvocations = new();

    private DotNetObjectReference<WebWorkersInterop>? _dotNetReference;
    private bool _disposed;
    private bool _initialized;

    public WebWorkersInterop(IJSRuntime jsRuntime, IResourceLoader resourceLoader)
    {
        _jsRuntime = jsRuntime;
        _resourceLoader = resourceLoader;
        _moduleInitializer = new AsyncInitializer(InitializeModule);
        _jsInitializer = new AsyncInitializer(InitializeJs);
    }

    private async ValueTask InitializeModule(CancellationToken cancellationToken)
    {
        _ = await _resourceLoader.ImportModule(_modulePath, cancellationToken);
    }

    private DotNetObjectReference<WebWorkersInterop> GetOrCreateDotNetReference()
    {
        _dotNetReference ??= DotNetObjectReference.Create(this);
        return _dotNetReference;
    }

    private async ValueTask InitializeJs(CancellationToken cancellationToken)
    {
        await _moduleInitializer.Init(cancellationToken);
        await _jsRuntime.InvokeVoidAsync(_jsInitialize, cancellationToken, GetOrCreateDotNetReference());
        _initialized = true;
    }

    private async ValueTask EnsureInitialized(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
            await _jsInitializer.Init(linked);
    }

    public ValueTask Initialize(CancellationToken cancellationToken = default)
    {
        return EnsureInitialized(cancellationToken);
    }

    public async ValueTask CreatePool(WebWorkerPoolOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Backend == WebWorkerBackend.JavaScript && string.IsNullOrWhiteSpace(options.ScriptPath))
            throw new ArgumentException("Worker script path cannot be null or whitespace.", nameof(options));

        if (options.WorkerCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Worker count must be greater than zero.");

        options.Name = options.Backend == WebWorkerBackend.DotNet ? NormalizeDotNetPoolName(options.Name) : NormalizePoolName(options.Name);

        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            await EnsureInitialized(linked);
            await _jsRuntime.InvokeVoidAsync(_jsCreatePool, linked, JsonUtil.Serialize(options));
        }
    }

    public async ValueTask<bool> PoolExists(string poolName, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(poolName))
            throw new ArgumentException("Pool name cannot be null or whitespace.", nameof(poolName));

        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            await EnsureInitialized(linked);
            return await _jsRuntime.InvokeAsync<bool>(_jsPoolExists, linked, poolName, backend.ToString());
        }
    }

    public ValueTask<WebWorkerResult<TResult>> Run<TResult>(string poolName, string workloadName, object? payload = null,
        Func<WebWorkerJobProgress, ValueTask>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        return Run<TResult>(new WebWorkerRequest
        {
            PoolName = poolName,
            WorkloadName = workloadName,
            Payload = payload
        }, progressCallback, cancellationToken);
    }

    public ValueTask<WebWorkerResult<TResult>> Run<TResult>(string workloadName, object? payload = null,
        Func<WebWorkerJobProgress, ValueTask>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        return Run<TResult>(new WebWorkerRequest
        {
            WorkloadName = workloadName,
            Payload = payload
        }, progressCallback, cancellationToken);
    }

    public async ValueTask<WebWorkerResult<TResult>> Run<TResult>(WebWorkerRequest request,
        Func<WebWorkerJobProgress, ValueTask>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();
        bool isDotNet = request.Backend == WebWorkerBackend.DotNet;

        if (isDotNet)
        {
            if (string.IsNullOrWhiteSpace(request.MethodName))
                throw new ArgumentException("Method name cannot be null or whitespace.", nameof(request));

            bool useDefaultPool = string.IsNullOrWhiteSpace(request.PoolName);
            request.PoolName = NormalizeDotNetPoolName(request.PoolName);
            request.RequestId ??= Guid.NewGuid().ToString("N");
            request.Arguments ??= [];

            if (useDefaultPool)
                await EnsureDotNetPoolExistsForRun(request.PoolName, cancellationToken);

            CancellationToken linkedDotNet = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? dotNetSource);
            var pendingInvocation = new DotNetPendingInvocation<TResult>(request.RequestId);

            if (!_pendingInvocations.TryAdd(request.RequestId, pendingInvocation))
                throw new InvalidOperationException($"A request with id '{request.RequestId}' is already pending.");

            using CancellationTokenRegistration dotNetRegistration = cancellationToken.CanBeCanceled
                ? cancellationToken.Register(static state =>
                {
                    var registrationState = (DotNetCancellationRegistrationState)state!;
                    _ = registrationState.Interop.TryCancelDotNetFromTokenAsync(registrationState.PoolName, registrationState.InvocationId);
                }, new DotNetCancellationRegistrationState(this, request.PoolName, request.RequestId))
                : default;

            try
            {
                using (dotNetSource)
                {
                    await EnsureInitialized(linkedDotNet);
                    await _jsRuntime.InvokeVoidAsync(_jsRunRequest, linkedDotNet, JsonUtil.Serialize(request));
                }

                return await pendingInvocation.Task.WaitAsync(_cancellationScope.CancellationToken);
            }
            catch
            {
                _pendingInvocations.TryRemove(request.RequestId, out _);
                throw;
            }
        }

        if (string.IsNullOrWhiteSpace(request.WorkloadName))
            throw new ArgumentException("Workload name cannot be null or whitespace.", nameof(request));

        bool useDefaultJsPool = string.IsNullOrWhiteSpace(request.PoolName);
        request.PoolName = NormalizePoolName(request.PoolName);
        request.RequestId ??= Guid.NewGuid().ToString("N");

        if (useDefaultJsPool)
            await EnsurePoolExistsForRun(request.PoolName, cancellationToken);

        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);
        var pendingJob = new PendingJob<TResult>(request.RequestId, progressCallback);

        if (!_pendingJobs.TryAdd(request.RequestId, pendingJob))
            throw new InvalidOperationException($"A request with id '{request.RequestId}' is already pending.");

        using CancellationTokenRegistration registration = cancellationToken.CanBeCanceled
            ? cancellationToken.Register(static state =>
            {
                var registrationState = (CancellationRegistrationState)state!;
                _ = registrationState.Interop.TryCancelFromTokenAsync(registrationState.PoolName, registrationState.JobId);
            }, new CancellationRegistrationState(this, request.PoolName, request.RequestId))
            : default;

        try
        {
            using (source)
            {
                await EnsureInitialized(linked);
                await _jsRuntime.InvokeVoidAsync(_jsRunRequest, linked, JsonUtil.Serialize(request));
            }

            return await pendingJob.Task.WaitAsync(_cancellationScope.CancellationToken);
        }
        catch
        {
            _pendingJobs.TryRemove(request.RequestId, out _);
            throw;
        }
    }

    public async ValueTask CancelRequest(string poolName, string requestId, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(poolName))
            throw new ArgumentException("Pool name cannot be null or whitespace.", nameof(poolName));

        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request id cannot be null or whitespace.", nameof(requestId));

        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            await EnsureInitialized(linked);
            await _jsRuntime.InvokeVoidAsync(_jsCancelRequest, linked, poolName, requestId, backend.ToString());
        }
    }

    public async ValueTask DestroyPool(string poolName, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(poolName))
            throw new ArgumentException("Pool name cannot be null or whitespace.", nameof(poolName));

        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            await EnsureInitialized(linked);
            await _jsRuntime.InvokeVoidAsync(_jsDestroyPool, linked, poolName, backend.ToString());
        }
    }

    public async ValueTask<WebWorkerPoolSnapshot?> GetPoolSnapshot(string poolName, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(poolName))
            throw new ArgumentException("Pool name cannot be null or whitespace.", nameof(poolName));

        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            await EnsureInitialized(linked);
            string? json = await _jsRuntime.InvokeAsync<string?>(_jsGetPoolSnapshot, linked, poolName, backend.ToString());

            return string.IsNullOrWhiteSpace(json) || string.Equals(json, "null", StringComparison.OrdinalIgnoreCase)
                ? null
                : JsonUtil.Deserialize<WebWorkerPoolSnapshot>(json);
        }
    }

    public async ValueTask<IReadOnlyList<WebWorkerPoolSnapshot>> GetPoolSnapshots(WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            await EnsureInitialized(linked);
            string json = await _jsRuntime.InvokeAsync<string>(_jsGetPoolSnapshots, linked, backend.ToString());

            return JsonUtil.Deserialize<List<WebWorkerPoolSnapshot>>(json) ?? [];
        }
    }

    public async ValueTask<WebWorkerCoordinatorSnapshot> GetCoordinatorSnapshot(WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            await EnsureInitialized(linked);
            string json = await _jsRuntime.InvokeAsync<string>(_jsGetCoordinatorSnapshot, linked, backend.ToString());

            return JsonUtil.Deserialize<WebWorkerCoordinatorSnapshot>(json) ?? new WebWorkerCoordinatorSnapshot();
        }
    }

    [JSInvokable]
    public async Task HandleCoordinatorEvent(string eventJson)
    {
        if (string.IsNullOrWhiteSpace(eventJson))
            return;

        var coordinatorEvent = JsonUtil.Deserialize<CoordinatorEvent>(eventJson);

        if (coordinatorEvent == null || string.IsNullOrWhiteSpace(coordinatorEvent.RequestId))
            return;

        if (!_pendingJobs.TryGetValue(coordinatorEvent.RequestId, out IPendingJob? pendingJob))
            return;

        switch (coordinatorEvent.EventType)
        {
            case "progress":
                await pendingJob.ReportProgressAsync(coordinatorEvent.ToProgress());
                break;
            case "completed":
                pendingJob.SetCompleted(coordinatorEvent);
                _pendingJobs.TryRemove(coordinatorEvent.RequestId, out _);
                break;
            case "cancelled":
                pendingJob.SetCancelled(coordinatorEvent);
                _pendingJobs.TryRemove(coordinatorEvent.RequestId, out _);
                break;
            case "faulted":
                pendingJob.SetFaulted(coordinatorEvent);
                _pendingJobs.TryRemove(coordinatorEvent.RequestId, out _);
                break;
        }
    }

    [JSInvokable]
    public async Task HandleDotNetCoordinatorEvent(string eventJson)
    {
        if (string.IsNullOrWhiteSpace(eventJson))
            return;

        var coordinatorEvent = JsonUtil.Deserialize<CoordinatorEvent>(eventJson);

        if (coordinatorEvent == null || string.IsNullOrWhiteSpace(coordinatorEvent.RequestId))
            return;

        if (!_pendingInvocations.TryGetValue(coordinatorEvent.RequestId, out IDotNetPendingInvocation? pendingInvocation))
            return;

        switch (coordinatorEvent.EventType)
        {
            case "completed":
                await pendingInvocation.SetCompletedAsync(coordinatorEvent);
                _pendingInvocations.TryRemove(coordinatorEvent.RequestId, out _);
                break;
            case "cancelled":
                pendingInvocation.SetCancelled(coordinatorEvent);
                _pendingInvocations.TryRemove(coordinatorEvent.RequestId, out _);
                break;
            case "faulted":
                pendingInvocation.SetFaulted(coordinatorEvent);
                _pendingInvocations.TryRemove(coordinatorEvent.RequestId, out _);
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cancellationScope.Cancel();

        foreach (KeyValuePair<string, IPendingJob> pair in _pendingJobs)
        {
            pair.Value.SetDisposed();
        }

        foreach (KeyValuePair<string, IDotNetPendingInvocation> pair in _pendingInvocations)
        {
            pair.Value.SetDisposed();
        }

        _pendingJobs.Clear();
        _pendingInvocations.Clear();

        if (_initialized)
            await _jsRuntime.InvokeVoidAsync(_jsDispose);

        _dotNetReference?.Dispose();
        _dotNetReference = null;

        await _resourceLoader.DisposeModule(_modulePath);
        await _jsInitializer.DisposeAsync();
        await _moduleInitializer.DisposeAsync();
        await _cancellationScope.DisposeAsync();
    }

    internal async Task TryCancelFromTokenAsync(string poolName, string jobId)
    {
        try
        {
            await CancelRequest(poolName, jobId);
        }
        catch
        {
        }
    }

    internal async Task TryCancelDotNetFromTokenAsync(string poolName, string invocationId)
    {
        try
        {
            await CancelRequest(poolName, invocationId, WebWorkerBackend.DotNet);
        }
        catch
        {
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static string NormalizePoolName(string? poolName)
    {
        return string.IsNullOrWhiteSpace(poolName) ? WebWorkersDefaults.DefaultPoolName : poolName;
    }

    private static string NormalizeDotNetPoolName(string? poolName)
    {
        return string.IsNullOrWhiteSpace(poolName) ? DotNetWebWorkersDefaults.DefaultPoolName : poolName;
    }

    private async ValueTask EnsurePoolExistsForRun(string poolName, CancellationToken linkedCancellationToken)
    {
        if (await PoolExists(poolName, cancellationToken: linkedCancellationToken))
            return;

        throw new InvalidOperationException(
            $"Worker pool '{poolName}' does not exist. Create the pool first and supply a worker ScriptPath.");
    }

    private async ValueTask EnsureDotNetPoolExistsForRun(string poolName, CancellationToken linkedCancellationToken)
    {
        if (await PoolExists(poolName, WebWorkerBackend.DotNet, linkedCancellationToken))
            return;

        await CreatePool(new WebWorkerPoolOptions
        {
            Backend = WebWorkerBackend.DotNet,
            Name = poolName
        }, linkedCancellationToken);
    }
}
