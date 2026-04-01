using Soenneker.Blazor.WebWorkers.Enums;

namespace Soenneker.Blazor.WebWorkers.Dtos;

/// <summary>
/// Describes a queued request for either the JavaScript or .NET worker backend.
/// </summary>
public sealed class WebWorkerRequest : WebWorkerExecutionRequest
{
    public WebWorkerRequest()
    {
        Backend = WebWorkerBackend.JavaScript;
    }
}
