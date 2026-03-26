using BenchmarkDotNet.Attributes;

namespace Maple.Native.Benchmarks;

public class NativeBench
{
    [Benchmark]
    public void Empty() => Native.Empty();
}
