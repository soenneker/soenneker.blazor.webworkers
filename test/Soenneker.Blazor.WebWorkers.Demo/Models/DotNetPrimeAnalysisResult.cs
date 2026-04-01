namespace Soenneker.Blazor.WebWorkers.Demo.Models;

public sealed class DotNetPrimeAnalysisResult
{
    public int UpperBound { get; set; }
    public int PrimeCount { get; set; }
    public int LargestPrime { get; set; }
    public int Checksum { get; set; }
    public long ElapsedMs { get; set; }
}
