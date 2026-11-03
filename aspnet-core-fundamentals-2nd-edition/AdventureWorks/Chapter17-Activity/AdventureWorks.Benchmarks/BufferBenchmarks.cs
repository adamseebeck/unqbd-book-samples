using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace AdventureWorks.Benchmarks;

// Activity: a line buffer per call, new or rented. Rent returns
// an array at least as long as asked for; Return hands it back.
[MemoryDiagnoser]
public class BufferBenchmarks
{
    [Benchmark(Baseline = true)]
    public int NewArray()
    {
        var buffer = new char[1000];
        buffer[0] = 'x';
        return buffer.Length;
    }

    [Benchmark]
    public int Rented()
    {
        var buffer = ArrayPool<char>.Shared.Rent(1000);
        try
        {
            buffer[0] = 'x';
            return buffer.Length;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }
}
