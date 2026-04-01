using System;
using System.Text.Json;
using Soenneker.Blazor.WebWorkers.Enums;

namespace Soenneker.Blazor.WebWorkers.Dtos;

internal class CoordinatorEvent
{
    public WebWorkerBackend Backend { get; set; }
    public string? EventType { get; set; }
    public string? PoolName { get; set; }
    public string? RequestId { get; set; }
    public string? WorkloadName { get; set; }
    public string? MethodName { get; set; }
    public string? WorkerId { get; set; }
    public double Percent { get; set; }
    public int CompletedUnits { get; set; }
    public int TotalUnits { get; set; }
    public string? Stage { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public double DurationMs { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public JsonElement Result { get; set; }

    public string? JobId
    {
        get => RequestId;
        set => RequestId = value;
    }

    public string? InvocationId
    {
        get => RequestId;
        set => RequestId = value;
    }

    public WebWorkerJobProgress ToProgress()
    {
        return new WebWorkerJobProgress
        {
            PoolName = PoolName ?? string.Empty,
            JobId = RequestId ?? string.Empty,
            WorkloadName = WorkloadName ?? string.Empty,
            WorkerId = WorkerId ?? string.Empty,
            Percent = Percent,
            CompletedUnits = CompletedUnits,
            TotalUnits = TotalUnits,
            Stage = Stage,
            Message = Message,
            TimestampUtc = TimestampUtc
        };
    }
}
