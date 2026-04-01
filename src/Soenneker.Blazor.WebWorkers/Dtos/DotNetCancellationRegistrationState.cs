namespace Soenneker.Blazor.WebWorkers.Dtos;

internal sealed record DotNetCancellationRegistrationState(WebWorkersInterop Interop, string PoolName, string InvocationId);
