[![](https://img.shields.io/nuget/v/soenneker.blazor.webworkers.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.webworkers/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.webworkers/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.webworkers/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.blazor.webworkers.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.webworkers/)
[![](https://img.shields.io/badge/Demo-Live-blueviolet?style=for-the-badge&logo=github)](https://soenneker.github.io/soenneker.blazor.webworkers)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.webworkers/codeql.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.webworkers/actions/workflows/codeql.yml)

# Soenneker.Blazor.WebWorkers

Runs CPU-heavy JavaScript workloads or exported C# methods in managed browser-worker pools without blocking Blazor's UI thread.

## Install

```bash
dotnet add package Soenneker.Blazor.WebWorkers
```

Register and inject the scoped utility:

```csharp
using Soenneker.Blazor.WebWorkers.Registrars;

builder.Services.AddWebWorkersUtilAsScoped();
```

```razor
@using Soenneker.Blazor.WebWorkers.Abstract
@inject IWebWorkersUtil WebWorkers
```

Operations initialize the interop on demand. `Initialize()` is optional and only preloads it.

## JavaScript workers

Create a pool for a worker script served by your application:

```csharp
using Soenneker.Blazor.WebWorkers.Options;

await WebWorkers.CreatePool(new WebWorkerPoolOptions
{
    WorkerCount = 4,
    ScriptPath = "js/workers/app.worker.js"
});
```

This creates the pool named `default`. Queue a workload and structured-clone-compatible payload:

```csharp
using System.Text.Json;
using Soenneker.Blazor.WebWorkers.Dtos;

WebWorkerResult<JsonElement> result = await WebWorkers.Run<JsonElement>(
    "prime-analysis",
    new { upperBound = 180_000 },
    progress =>
    {
        Console.WriteLine($"{progress.Percent:0}% — {progress.Message}");
        return ValueTask.CompletedTask;
    },
    cancellationToken);
```

Worker failures and cooperative cancellations are returned in `WebWorkerResult<T>`; inspect `State`, `Result`, and `ErrorMessage`. Cancelling the .NET caller can throw `OperationCanceledException` while it waits.

### Worker protocol

Your worker receives `run` and `cancel` messages. It must send a terminal `completed`, `cancelled`, or `faulted` message with the same `jobId`:

```javascript
let activeJobId = null;
let cancellationRequested = false;
const yieldToWorker = () => new Promise(resolve => setTimeout(resolve, 0));

self.onmessage = async event => {
    const message = event.data;

    if (message?.type === "cancel" && message.jobId === activeJobId) {
        cancellationRequested = true;
        return;
    }

    if (message?.type !== "run") return;

    activeJobId = message.jobId;
    cancellationRequested = false;

    try {
        let total = 0;

        for (let index = 0; index < message.payload.count; index++) {
            if (cancellationRequested) throw new Error("cancelled");
            total += index;

            if (index % 1000 === 0) {
                self.postMessage({
                    type: "progress",
                    jobId: message.jobId,
                    percent: (index / message.payload.count) * 100,
                    completedUnits: index,
                    totalUnits: message.payload.count
                });

                await yieldToWorker();
            }
        }

        self.postMessage({ type: "completed", jobId: message.jobId, result: total });
    } catch (error) {
        self.postMessage({
            type: cancellationRequested ? "cancelled" : "faulted",
            jobId: message.jobId,
            errorMessage: error instanceof Error ? error.message : "Worker failed."
        });
    } finally {
        activeJobId = null;
        cancellationRequested = false;
    }
};

self.postMessage({ type: "ready" });
```

A cancel message cannot be observed during one uninterrupted synchronous loop. Split CPU work into chunks and yield to the worker event loop. `TimeoutMs` is enforced by terminating the worker; a timed-out result has `Faulted` state.

Use `WorkerType = WebWorkerScriptType.Module` when the script imports ES modules. For a worker shipped by a Razor class library:

```csharp
string scriptPath = WebWorkerAssetPaths.WorkerFromPackage(
    "My.Worker.Package",
    "image.worker.js");
```

### Named pools and cancellation

Set `WebWorkerPoolOptions.Name` to isolate scripts or concurrency limits. Assign a request ID when another handler needs to cancel the job:

```csharp
var request = new WebWorkerRequest
{
    PoolName = "images",
    RequestId = $"thumbnail-{imageId}",
    WorkloadName = "generate-thumbnail",
    Payload = new { imageId },
    TimeoutMs = 15_000
};

Task<WebWorkerResult<JsonElement>> running =
    WebWorkers.Run<JsonElement>(request).AsTask();

await WebWorkers.CancelRequest("images", request.RequestId);
WebWorkerResult<JsonElement> result = await running;
```

JavaScript cancellation is cooperative unless a timeout terminates the worker. A running .NET request is cancelled by terminating and replacing its worker.

## .NET workers

The .NET backend starts another WebAssembly runtime inside each browser worker and invokes `[JSExport]` methods from the main application assembly. It is for Blazor WebAssembly and costs more startup time and memory than a JavaScript worker.

Enable unsafe blocks in the application project:

```xml
<PropertyGroup>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```

Define a static exported method with JavaScript-interoperable arguments and results:

```csharp
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;

[SupportedOSPlatform("browser")]
public static partial class WorkerExports
{
    [JSExport]
    public static async Task<string> AnalyzePrimeRange(int upperBound)
    {
        await Task.Yield();
        return JsonSerializer.Serialize(new { upperBound });
    }
}
```

The expression overload extracts the static method and argument values; it does not run the method on the UI thread:

```csharp
WebWorkerResult<string> result =
    await WebWorkers.Run(() => WorkerExports.AnalyzePrimeRange(220_000));
```

With no .NET pool name, the default one-worker pool is created automatically. Create it explicitly to configure concurrency:

```csharp
await WebWorkers.CreatePool(new WebWorkerPoolOptions
{
    Backend = WebWorkerBackend.DotNet,
    WorkerCount = 2
});
```

The expression must be a direct call to a static `Task`-returning method. A manual request must select the .NET backend, use the fully qualified exported `MethodName`, and provide `Arguments` in declaration order.

## Inspect and destroy pools

Snapshots are point-in-time diagnostics:

```csharp
WebWorkerPoolSnapshot? pool = await WebWorkers.GetPoolSnapshot("default");
IReadOnlyList<WebWorkerPoolSnapshot> pools = await WebWorkers.GetPoolSnapshots();
WebWorkerCoordinatorSnapshot snapshot = await WebWorkers.GetCoordinatorSnapshot();

await WebWorkers.DestroyPool("default");
```

Destroying or replacing a pool terminates its workers and returns `Cancelled` results for attached requests. Disposing the scoped utility cleans up every pool.

## Security and browser constraints

- Worker scripts execute with your application's origin privileges. Use trusted, application-controlled script paths; never accept one from user input.
- Payloads use the structured-clone algorithm. Functions, DOM nodes, and many runtime-specific objects cannot be sent by this API.
- Transfer lists, `SharedArrayBuffer`, and direct DOM access are not exposed.
- Worker count multiplies memory use, especially for the .NET backend. Start small and measure on supported devices.
- Browser workers are unavailable during server prerendering and in Blazor Server. Start pools only after WebAssembly is running.
