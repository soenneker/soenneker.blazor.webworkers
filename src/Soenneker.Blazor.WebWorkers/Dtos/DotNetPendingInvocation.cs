using System;
using System.Text.Json;
using System.Threading.Tasks;
using Soenneker.Blazor.WebWorkers.Dtos.Abstract;
using Soenneker.Blazor.WebWorkers.Enums;
using Soenneker.Utils.Json;

namespace Soenneker.Blazor.WebWorkers.Dtos;

internal sealed class DotNetPendingInvocation<TResult> : IDotNetPendingInvocation
{
    private readonly TaskCompletionSource<WebWorkerResult<TResult>> _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal DotNetPendingInvocation(string invocationId)
    {
        InvocationId = invocationId;
    }

    internal string InvocationId { get; }
    internal Task<WebWorkerResult<TResult>> Task => _taskCompletionSource.Task;

    public ValueTask SetCompletedAsync(CoordinatorEvent coordinatorEvent)
    {
        _taskCompletionSource.TrySetResult(new WebWorkerResult<TResult>
        {
            PoolName = coordinatorEvent.PoolName ?? string.Empty,
            Backend = WebWorkerBackend.DotNet,
            RequestId = coordinatorEvent.RequestId ?? InvocationId,
            MethodName = coordinatorEvent.MethodName ?? string.Empty,
            WorkerId = coordinatorEvent.WorkerId,
            State = WebWorkerJobState.Completed,
            Result = DeserializeResult(coordinatorEvent.Result),
            DurationMs = coordinatorEvent.DurationMs,
            CompletedAtUtc = coordinatorEvent.TimestampUtc
        });

        return ValueTask.CompletedTask;
    }

    public void SetCancelled(CoordinatorEvent coordinatorEvent)
    {
        _taskCompletionSource.TrySetResult(new WebWorkerResult<TResult>
        {
            PoolName = coordinatorEvent.PoolName ?? string.Empty,
            Backend = WebWorkerBackend.DotNet,
            RequestId = coordinatorEvent.RequestId ?? InvocationId,
            MethodName = coordinatorEvent.MethodName ?? string.Empty,
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
            Backend = WebWorkerBackend.DotNet,
            RequestId = coordinatorEvent.RequestId ?? InvocationId,
            MethodName = coordinatorEvent.MethodName ?? string.Empty,
            WorkerId = coordinatorEvent.WorkerId,
            State = WebWorkerJobState.Faulted,
            ErrorMessage = coordinatorEvent.ErrorMessage,
            DurationMs = coordinatorEvent.DurationMs,
            CompletedAtUtc = coordinatorEvent.TimestampUtc
        });
    }

    public void SetDisposed()
    {
        _taskCompletionSource.TrySetException(new ObjectDisposedException("DotNetWorkerInterop"));
    }

    private static TResult? DeserializeResult(JsonElement result)
    {
        if (result.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return default;

        if (result.ValueKind == JsonValueKind.String)
        {
            string rawString = result.GetString() ?? string.Empty;

            if (typeof(TResult) == typeof(JsonElement))
                return (TResult)(object)(LooksLikeJson(rawString) ? JsonUtil.Deserialize<JsonElement>(rawString) : result);

            if (typeof(TResult) == typeof(string))
                return (TResult)(object)rawString;

            if (typeof(TResult).IsPrimitive || typeof(TResult).IsEnum)
                return JsonUtil.Deserialize<TResult>($"\"{rawString}\"");

            if (LooksLikeJson(rawString))
                return JsonUtil.Deserialize<TResult>(rawString);
        }

        if (typeof(TResult) == typeof(JsonElement))
            return (TResult)(object)result;

        return JsonUtil.Deserialize<TResult>(result.GetRawText());
    }

    private static bool LooksLikeJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        char start = value.TrimStart()[0];
        return start is '{' or '[' or '"';
    }
}
