namespace Soenneker.Blazor.WebWorkers.Dtos;

internal sealed record CancellationRegistrationState(WebWorkersInterop Interop, string PoolName, string JobId);
