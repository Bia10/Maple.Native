using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Maple.Native.ComparisonBenchmarks;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory, BenchmarkLogicalGroupRule.ByParams)]
[BenchmarkCategory("0")]
public class TestBench
{
    [Params(25_000)]
    public int Count { get; set; }

    [Benchmark(Baseline = true)]
    public void Maple_Native______()
    {
        // Baseline: call the core operation you're benchmarking
        Native.Empty();
    }
}
