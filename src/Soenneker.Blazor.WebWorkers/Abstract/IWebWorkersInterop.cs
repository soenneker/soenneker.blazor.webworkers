using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Blazor.WebWorkers.Dtos;
using Soenneker.Blazor.WebWorkers.Enums;
using Soenneker.Blazor.WebWorkers.Options;

namespace Soenneker.Blazor.WebWorkers.Abstract;

/// <summary>
/// Blazor interop for browser-facing worker orchestration functionality.
/// </summary>
public interface IWebWorkersInterop : IAsyncDisposable
{
    /// <summary>
    /// Ensures the JavaScript module for this package has been loaded and initialized.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the Web Workers is ready for use.</returns>
    ValueTask Initialize(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or replaces a named worker pool.
    /// </summary>
    /// <param name="options">Options to configure for the web workers.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the pool creation is complete.</returns>
    ValueTask CreatePool(WebWorkerPoolOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates whether a named worker pool exists.
    /// </summary>
    /// <param name="poolName">Name of the target pool.</param>
    /// <param name="backend">Backend implementation that performs the operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>true if indicates whether a named worker pool exists; otherwise, false.</returns>
    ValueTask<bool> PoolExists(string poolName, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues work on a named pool and awaits the terminal result.
    /// </summary>
    /// <typeparam name="TResult">Type of result produced by the operation.</typeparam>
    /// <param name="request">request that defines the request to send.</param>
    /// <param name="progressCallback">progress Callback to invoke when the operation runs.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested web Worker Result.</returns>
    ValueTask<WebWorkerResult<TResult>> Run<TResult>(WebWorkerRequest request, Func<WebWorkerJobProgress, ValueTask>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues work on a named pool using a lightweight call shape.
    /// </summary>
    /// <typeparam name="TResult">Type of result produced by the operation.</typeparam>
    /// <param name="poolName">Name of the target pool.</param>
    /// <param name="workloadName">Name of the workload to target.</param>
    /// <param name="payload">Payload processed by the operation.</param>
    /// <param name="progressCallback">progress Callback to invoke when the operation runs.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested web Worker Result.</returns>
    ValueTask<WebWorkerResult<TResult>> Run<TResult>(string poolName, string workloadName, object? payload = null,
        Func<WebWorkerJobProgress, ValueTask>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues work on the package default pool using a lightweight call shape.
    /// </summary>
    /// <typeparam name="TResult">Type of result produced by the operation.</typeparam>
    /// <param name="workloadName">Name of the workload to target.</param>
    /// <param name="payload">Payload processed by the operation.</param>
    /// <param name="progressCallback">progress Callback to invoke when the operation runs.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested web Worker Result.</returns>
    ValueTask<WebWorkerResult<TResult>> Run<TResult>(string workloadName, object? payload = null,
        Func<WebWorkerJobProgress, ValueTask>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests cooperative cancellation for a queued or running request.
    /// </summary>
    /// <param name="poolName">Name of the target pool.</param>
    /// <param name="requestId">request ID that defines the request to send.</param>
    /// <param name="backend">Backend implementation that performs the operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the cancel request operation is complete.</returns>
    ValueTask CancelRequest(string poolName, string requestId, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tears down a pool and cancels any work still attached to it.
    /// </summary>
    /// <param name="poolName">Name of the target pool.</param>
    /// <param name="backend">Backend implementation that performs the operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the destroy pool operation is complete.</returns>
    ValueTask DestroyPool(string poolName, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a snapshot for a specific pool if it exists.
    /// </summary>
    /// <param name="poolName">Name of the target pool.</param>
    /// <param name="backend">Backend implementation that performs the operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested web Worker Pool Snapshot.</returns>
    ValueTask<WebWorkerPoolSnapshot?> GetPoolSnapshot(string poolName, WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns snapshots for all current pools.
    /// </summary>
    /// <param name="backend">Backend implementation that performs the operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the collection returned by get Pool Snapshots.</returns>
    ValueTask<IReadOnlyList<WebWorkerPoolSnapshot>> GetPoolSnapshots(WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a top-level snapshot of the coordinator and all pools.
    /// </summary>
    /// <param name="backend">Backend implementation that performs the operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested web Worker Coordinator Snapshot.</returns>
    ValueTask<WebWorkerCoordinatorSnapshot> GetCoordinatorSnapshot(WebWorkerBackend backend = WebWorkerBackend.JavaScript,
        CancellationToken cancellationToken = default);
}
