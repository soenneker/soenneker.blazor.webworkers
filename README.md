[![](https://img.shields.io/nuget/v/soenneker.blazor.webworkers.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.webworkers/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.webworkers/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.webworkers/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.blazor.webworkers.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.webworkers/)
[![](https://img.shields.io/badge/Demo-Live-blueviolet?style=for-the-badge&logo=github)](https://soenneker.github.io/soenneker.blazor.webworkers)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.webworkers/codeql.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.webworkers/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Blazor.WebWorkers
### Run background work in Blazor without freezing the UI.

This package gives you one simple API for browser web workers in Blazor.

Use it when you want to:

- move CPU-heavy work off the UI thread
- keep the app responsive while work is running
- run custom JavaScript worker jobs
- run exported C# methods in JS
- monitor progress, cancel requests, and inspect worker pool state

## Install

```bash
dotnet add package Soenneker.Blazor.WebWorkers
```

## The Basic Idea

`Soenneker.Blazor.WebWorkers` manages worker pools for you.

In most apps, the flow is:

1. Register `IWebWorkersUtil`
2. Call `Initialize()`
3. Create a worker pool
4. Queue work
5. Optionally monitor progress, cancel work, or inspect snapshots

The same `IWebWorkersUtil` service works for both:

- JavaScript workers
- `.NET` workers

## Quick Start

Register the service in `Program.cs`:

```csharp
builder.Services.AddWebWorkersUtilAsScoped();
```

Inject it where you want to use it:

```csharp
@inject IWebWorkersUtil WebWorkers
```

Initialize it once before first use:

```csharp
await WebWorkers.Initialize();
```

## Common Workflow

### 1. Create a JavaScript worker pool

Point the pool at your worker script:

```csharp
await WebWorkers.CreatePool(new WebWorkerPoolOptions
{
    WorkerCount = 4,
    ScriptPath = "js/workers/app.worker.js"
});
```

That creates the default JavaScript pool.

### 2. Queue a JavaScript job

Pass a workload name and payload:

```csharp
using System.Text.Json;
using Soenneker.Blazor.WebWorkers.Dtos;

WebWorkerResult<JsonElement> result = await WebWorkers.Run<JsonElement>(
    "prime-analysis",
    new
    {
        upperBound = 180000
    },
    progress =>
    {
        Console.WriteLine($"{progress.Percent:0}% - {progress.Message}");
        return ValueTask.CompletedTask;
    });
```

Your worker script is responsible for understanding the workload name and payload.

### 3. Cancel or inspect work

```csharp
await WebWorkers.CancelRequest("default", jobId);

WebWorkerCoordinatorSnapshot snapshot = await WebWorkers.GetCoordinatorSnapshot();
```

## JavaScript Worker Path

This is the best fit when your worker logic already lives in JavaScript, or when you want full control over the worker script.

Important points:

- JavaScript pools use `WebWorkerBackend.JavaScript`
- the default pool name is `"default"`
- you usually only need `WorkerCount` and `ScriptPath`
- jobs are queued with a `workloadName` and optional `payload`
- you can report progress back while work is running

You can also target a specific named pool:

```csharp
await WebWorkers.CreatePool(new WebWorkerPoolOptions
{
    Name = "images",
    WorkerCount = 2,
    ScriptPath = "js/workers/image.worker.js"
});

WebWorkerResult<JsonElement> result = await WebWorkers.Run<JsonElement>(
    "images",
    "generate-thumbnail",
    new
    {
        width = 300,
        height = 300
    });
```

### Worker scripts from a Razor class library

If a worker file ships from an RCL, build the static asset path like this:

```csharp
string workerPath = WebWorkerAssetPaths.WorkerFromPackage(
    "Soenneker.Blazor.Opfs",
    "opfs.worker.js");
```

Then use that path as the pool's `ScriptPath`.

## .NET Worker Path

This package can also run exported C# methods inside a browser worker by booting a second .NET WebAssembly runtime in that worker.

This path is useful when:

- your work is already written in C#
- you want to keep background logic in your Blazor app
- you do not want to hand-write a JavaScript worker for that job

### Requirements

The `.NET` worker path requires:

- Blazor WebAssembly
- `AllowUnsafeBlocks=true` in the app `.csproj`
- exported worker methods defined in the main app assembly
- worker methods marked with `[JSExport]`

Example `.csproj` setting:

```xml
<PropertyGroup>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```

### Define an exported worker method

```csharp
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Soenneker.Utils.Json;

[SupportedOSPlatform("browser")]
public static partial class WorkerExports
{
    [JSExport]
    public static string? AnalyzePrimeRange(int upperBound)
    {
        return JsonUtil.Serialize(new
        {
            upperBound,
            message = "Ran inside a .NET worker."
        });
    }
}
```

### Create a .NET worker pool

```csharp
using Soenneker.Blazor.WebWorkers.Enums;
using Soenneker.Blazor.WebWorkers.Options;

await WebWorkers.CreatePool(new WebWorkerPoolOptions
{
    Backend = WebWorkerBackend.DotNet,
    WorkerCount = 1
});
```

### Invoke the exported method

You can call it with a request object:

```csharp
using Soenneker.Blazor.WebWorkers.Dtos;
using Soenneker.Blazor.WebWorkers.Enums;

WebWorkerResult<string?> result = await WebWorkers.Run<string?>(new WebWorkerRequest
{
    Backend = WebWorkerBackend.DotNet,
    MethodName = "MyApp.WorkerExports.AnalyzePrimeRange",
    Arguments = [220000]
});
```

Or use the expression-based overload:

```csharp
WebWorkerResult<MyResult> result =
    await WebWorkers.Run(() => WorkerExports.RunAnalysisAsync(220000));
```

The expression-based overload is often the easiest option because it avoids building the request manually.

## Important Types

### `IWebWorkersUtil`

The main service you work with. It handles:

- initialization
- pool creation and destruction
- job execution
- cancellation
- pool and coordinator snapshots

### `WebWorkerPoolOptions`

Used when creating a pool.

The most important properties are:

- `Backend`
- `Name`
- `ScriptPath`
- `WorkerCount`
- `WorkerType`
- `RuntimeScriptPath`
- `BootConfigPath`
- `RestartFaultedWorkers`

### `WebWorkerRequest`

Used when you need full control over a queued request.

The most important properties are:

- `PoolName`
- `Backend`
- `RequestId`
- `WorkloadName`
- `MethodName`
- `Payload`
- `Arguments`
- `TimeoutMs`

## Monitoring and Cancellation

You can:

- receive progress callbacks while a job is running
- cancel a queued or running request
- inspect one pool or all pools
- inspect a full coordinator snapshot

Examples:

```csharp
await WebWorkers.CancelRequest("default", requestId);

WebWorkerPoolSnapshot? pool = await WebWorkers.GetPoolSnapshot("default");
IReadOnlyList<WebWorkerPoolSnapshot> pools = await WebWorkers.GetPoolSnapshots();
WebWorkerCoordinatorSnapshot snapshot = await WebWorkers.GetCoordinatorSnapshot();
```

## Things To Know

- JavaScript and `.NET` workers share the same top-level service: `IWebWorkersUtil`
- JavaScript jobs use `WorkloadName` plus `Payload`
- `.NET` jobs use `MethodName` plus `Arguments`
- the `.NET` worker path is for Blazor WebAssembly
- `.NET` worker methods should usually return simple values or serialized JSON
- if your worker code naturally returns `ValueTask`, expose a small `Task`-returning `[JSExport]` wrapper
- cancellation of a running `.NET` worker request may require terminating and replacing the backing worker

## Which Path Should I Use?

Use the JavaScript path when:

- you already have worker logic in JavaScript
- you need a custom browser worker script
- your workload is naturally message-based

Use the `.NET` path when:

- your workload is already implemented in C#
- you want to stay in C# as much as possible
- you are building a Blazor WebAssembly app (Not available in Server)