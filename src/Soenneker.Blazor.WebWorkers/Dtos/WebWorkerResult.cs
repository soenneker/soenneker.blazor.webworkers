namespace Soenneker.Blazor.WebWorkers.Dtos;

/// <summary>
/// Terminal result for a completed request from either worker backend.
/// </summary>
public sealed class WebWorkerResult<TResult> : WebWorkerExecutionResult<TResult>
{
}
