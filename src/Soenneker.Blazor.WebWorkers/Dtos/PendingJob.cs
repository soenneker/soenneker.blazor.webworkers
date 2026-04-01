using System;
using System.Text.Json;
using System.Threading.Tasks;
using Soenneker.Blazor.WebWorkers.Dtos.Abstract;
using Soenneker.Blazor.WebWorkers.Enums;
using Soenneker.Utils.Json;

namespace Soenneker.Blazor.WebWorkers.Dtos;

internal sealed class PendingJob<TResult> : IPendingJob
{
    private readonly Func<WebWorkerJobProgress, ValueTask>? _progressCallback;
    private readonly TaskCompletionSource<WebWorkerResult<TResult>> _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal PendingJob(string jobId, Func<WebWorkerJobProgress, ValueTask>? progressCallback)
    {
        JobId = jobId;
        _progressCallback = progressCallback;
    }

    internal string JobId { get; }
    internal Task<WebWorkerResult<TResult>> Task => _taskCompletionSource.Task;

    public async ValueTask ReportProgressAsync(WebWorkerJobProgress progress)
    {
        if (_progressCallback == null)
            return;

        try
        {
            await _progressCallback(progress);
        }
        catch
        {
        }
    }

    public void SetCompleted(CoordinatorEvent coordinatorEvent)
    {
        _taskCompletionSource.TrySetResult(new WebWorkerResult<TResult>
        {
            PoolName = coordinatorEvent.PoolName ?? string.Empty,
            Backend = WebWorkerBackend.JavaScript,
            RequestId = coordinatorEvent.RequestId ?? JobId,
            WorkloadName = coordinatorEvent.WorkloadName ?? string.Empty,
            WorkerId = coordinatorEvent.WorkerId,
            State = WebWorkerJobState.Completed,
            Result = DeserializeResult(coordinatorEvent.Result),
            DurationMs = coordinatorEvent.DurationMs,
            CompletedAtUtc = coordinatorEvent.TimestampUtc
        });
    }

    public void SetCancelled(CoordinatorEvent coordinatorEvent)
    {
        _taskCompletionSource.TrySetResult(new WebWorkerResult<TResult>
        {
            PoolName = coordinatorEvent.PoolName ?? string.Empty,
            Backend = WebWorkerBackend.JavaScript,
            RequestId = coordinatorEvent.RequestId ?? JobId,
            WorkloadName = coordinatorEvent.WorkloadName ?? string.Empty,
            WorkerId = coordinatorEvent.WorkerId,
            State = WebWorkerJobState.Cancelled,
            ErrorMessage = coordinatorEvent.ErrorMessage,
            DurationMs = coordinatorEvent.DurationMs,
            CompletedAtUtc = coordinatorEvent.TimestampUtc
        });
    }

    public void SetFaulted(CoordinatorEvent coordinatorEvent)
    {
        _taskCompletionSource.TrySetResult(new WebWorkerResult<TResult>
        {
            PoolName = coordinatorEvent.PoolName ?? string.Empty,
            Backend = WebWorkerBackend.JavaScript,
            RequestId = coordinatorEvent.RequestId ?? JobId,
            WorkloadName = coordinatorEvent.WorkloadName ?? string.Empty,
            WorkerId = coordinatorEvent.WorkerId,
            State = WebWorkerJobState.Faulted,
            ErrorMessage = coordinatorEvent.ErrorMessage,
            DurationMs = coordinatorEvent.DurationMs,
            CompletedAtUtc = coordinatorEvent.TimestampUtc
        });
    }

    public void SetDisposed()
    {
        _taskCompletionSource.TrySetException(new ObjectDisposedException("WebWorkersInterop"));
    }

    private static TResult? DeserializeResult(JsonElement result)
    {
        if (result.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return default;

        if (typeof(TResult) == typeof(JsonElement))
            return (TResult) (object) result;

        return JsonUtil.Deserialize<TResult>(result.GetRawText());
    }
}
