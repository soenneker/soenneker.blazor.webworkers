using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Soenneker.Blazor.WebWorkers.Demo.Models;
using Soenneker.Utils.Json;

namespace Soenneker.Blazor.WebWorkers.Demo.Workers;

[SupportedOSPlatform("browser")]
public static partial class DemoDotNetWorkerExports
{
    [JSExport]
    public static string? AnalyzePrimeRangeSync(int upperBound)
    {
        int limit = Math.Max(upperBound, 5000);
        var stopwatch = Stopwatch.StartNew();
        var primes = new List<int>();
        var checksum = 0;

        for (var candidate = 2; candidate <= limit; candidate++)
        {
            var isPrime = true;

            for (var index = 0; index < primes.Count; index++)
            {
                int divisor = primes[index];

                if (divisor * divisor > candidate)
                    break;

                if (candidate % divisor == 0)
                {
                    isPrime = false;
                    break;
                }
            }

            if (!isPrime)
                continue;

            primes.Add(candidate);
            checksum = (checksum + candidate) % 1000000007;
        }

        stopwatch.Stop();

        return JsonUtil.Serialize(new DotNetPrimeAnalysisResult
        {
            UpperBound = limit,
            PrimeCount = primes.Count,
            LargestPrime = primes.Count > 0 ? primes[^1] : 0,
            Checksum = checksum,
            ElapsedMs = stopwatch.ElapsedMilliseconds
        });
    }

    [JSExport]
    public static async Task<string?> AnalyzePrimeRange(int upperBound)
    {
        await Task.Yield();
        return AnalyzePrimeRangeSync(upperBound);
    }
}
