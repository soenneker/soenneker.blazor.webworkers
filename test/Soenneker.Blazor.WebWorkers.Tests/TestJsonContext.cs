using System.Text.Json;
using System.Text.Json.Serialization;

namespace 
Soenneker.Blazor.WebWorkers.Tests
;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(string))]
internal partial class TestJsonContext : JsonSerializerContext;
