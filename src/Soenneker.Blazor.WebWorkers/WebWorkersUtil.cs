using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Blazor.WebWorkers.Abstract;
using Soenneker.Blazor.WebWorkers.Dtos;
using Soenneker.Blazor.WebWorkers.Enums;
using Soenneker.Blazor.WebWorkers.Internals;
using Soenneker.Blazor.WebWorkers.Options;

namespace Soenneker.Blazor.WebWorkers;

/// <inheritdoc cref="IWebWorkersUtil"/>
public sealed class WebWorkersUtil : IWebWorkersUtil
{
    private readonly IWebWorkersInterop _interop;

    public WebWorkersUtil(IWebWorkersInterop interop)
    {
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask Initialize(CancellationToken cancellationToken = default)
    {
        return _interop.Initialize(cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask CreatePool(WebWorkerPoolOptions options, CancellationToken cancellationToken = default)
    {
        return _interop.CreatePool(options, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> PoolExists(string poolName, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default)
    {
        return _interop.PoolExists(poolName, backend, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<WebWorkerResult<TResult>> Run<TResult>(WebWorkerRequest request,
        Func<WebWorkerJobProgress, ValueTask>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        return _interop.Run<TResult>(request, progressCallback, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<WebWorkerResult<TResult>> Run<TResult>(string poolName, string workloadName, object? payload = null,
        Func<WebWorkerJobProgress, ValueTask>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        return _interop.Run<TResult>(poolName, workloadName, payload, progressCallback, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<WebWorkerResult<TResult>> Run<TResult>(string workloadName, object? payload = null,
        Func<WebWorkerJobProgress, ValueTask>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        return _interop.Run<TResult>(workloadName, payload, progressCallback, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<WebWorkerResult<TResult>> Run<TResult>(Expression<Func<Task<TResult>>> taskExpression,
        string? poolName = null, string? requestId = null, int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        WebWorkerRequest request = DotNetInvocationExpressionParser.Parse(taskExpression, poolName);
        request.RequestId = requestId;
        request.TimeoutMs = timeoutMs;
        return _interop.Run<TResult>(request, null, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<WebWorkerResult<object?>> Run(Expression<Func<Task>> taskExpression,
        string? poolName = null, string? requestId = null, int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        WebWorkerRequest request = DotNetInvocationExpressionParser.Parse(taskExpression, poolName);
        request.RequestId = requestId;
        request.TimeoutMs = timeoutMs;
        return _interop.Run<object?>(request, null, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask CancelRequest(string poolName, string requestId, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default)
    {
        return _interop.CancelRequest(poolName, requestId, backend, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask DestroyPool(string poolName, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default)
    {
        return _interop.DestroyPool(poolName, backend, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<WebWorkerPoolSnapshot?> GetPoolSnapshot(string poolName, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default)
    {
        return _interop.GetPoolSnapshot(poolName, backend, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<IReadOnlyList<WebWorkerPoolSnapshot>> GetPoolSnapshots(WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default)
    {
        return await _interop.GetPoolSnapshots(backend, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<WebWorkerCoordinatorSnapshot> GetCoordinatorSnapshot(WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default)
    {
        return _interop.GetCoordinatorSnapshot(backend, cancellationToken);
    }
}
