using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Blazor.WebWorkers.Dtos;
using Soenneker.Blazor.WebWorkers.Enums;
using Soenneker.Blazor.WebWorkers.Options;

namespace Soenneker.Blazor.WebWorkers.Abstract;

/// <summary>
/// A higher-level orchestration utility that fronts both JavaScript worker and .NET worker backends.
/// </summary>
public interface IWebWorkersUtil
{
    /// <summary>
    /// Ensures the underlying JavaScript module has been loaded and is ready for use.
    /// </summary>
    ValueTask Initialize(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or replaces a named worker pool.
    /// </summary>
    ValueTask CreatePool(WebWorkerPoolOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates whether a named worker pool exists.
    /// </summary>
    ValueTask<bool> PoolExists(string poolName, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues work on a named pool and awaits the terminal result.
    /// </summary>
    ValueTask<WebWorkerResult<TResult>> Run<TResult>(WebWorkerRequest request, Func<WebWorkerJobProgress, ValueTask>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues work on a named pool using a lightweight call shape.
    /// </summary>
    ValueTask<WebWorkerResult<TResult>> Run<TResult>(string poolName, string workloadName, object? payload = null,
        Func<WebWorkerJobProgress, ValueTask>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues work on the package default pool using a lightweight call shape.
    /// </summary>
    ValueTask<WebWorkerResult<TResult>> Run<TResult>(string workloadName, object? payload = null,
        Func<WebWorkerJobProgress, ValueTask>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues a static .NET worker method represented as a <see cref="Task{TResult}"/>-returning expression.
    /// </summary>
    ValueTask<WebWorkerResult<TResult>> Run<TResult>(Expression<Func<Task<TResult>>> taskExpression,
        string? poolName = null, string? requestId = null, int? timeoutMs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues a static .NET worker method represented as a <see cref="Task"/>-returning expression.
    /// </summary>
    ValueTask<WebWorkerResult<object?>> Run(Expression<Func<Task>> taskExpression,
        string? poolName = null, string? requestId = null, int? timeoutMs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests cooperative cancellation for a queued or running request.
    /// </summary>
    ValueTask CancelRequest(string poolName, string requestId, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tears down a pool and cancels any work still attached to it.
    /// </summary>
    ValueTask DestroyPool(string poolName, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a snapshot for a specific pool if it exists.
    /// </summary>
    ValueTask<WebWorkerPoolSnapshot?> GetPoolSnapshot(string poolName, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns snapshots for all current pools.
    /// </summary>
    ValueTask<IReadOnlyList<WebWorkerPoolSnapshot>> GetPoolSnapshots(WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a top-level snapshot of the coordinator and all pools.
    /// </summary>
    ValueTask<WebWorkerCoordinatorSnapshot> GetCoordinatorSnapshot(WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default);
}
