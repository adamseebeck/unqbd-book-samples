using System.Globalization;
using AdventureWorks.Core;
using BenchmarkDotNet.Attributes;

namespace AdventureWorks.Benchmarks;

// Two checks of the same 1,000 feed lines: Split makes an
// array and three strings a line; PriceFeed.IsRecord reads spans.
// Each returns its count, so the work cannot be optimized away.
[MemoryDiagnoser]
public class PriceFeedBenchmarks
{
    readonly string[] _lines = [.. Enumerable.Repeat(
        "HLM-1001,Trail Helmet,29.99", 1000)];

    [Benchmark(Baseline = true)]
    public int Split()
    {
        var valid = 0;
        foreach (var line in _lines)
        {
            var fields = line.Split(',');
            if (fields.Length == 3 && decimal.TryParse(fields[2],
                    CultureInfo.InvariantCulture, out _))
                valid++;
        }
        return valid;
    }

    [Benchmark]
    public int Span()
    {
        var valid = 0;
        foreach (var line in _lines)
            if (PriceFeed.IsRecord(line))
                valid++;
        return valid;
    }
}
