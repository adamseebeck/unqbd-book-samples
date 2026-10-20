using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class CsvParsingBenchmarks
{
    private readonly string[] _lines = Enumerable.Repeat("101,Notebook,12.50", 1000).ToArray();
    [Benchmark(Baseline = true)]
    public int Split()
    {
        int fields = 0;
        foreach (string line in _lines) fields += line.Split(',').Length;
        return fields;
    }
    [Benchmark]
    public int Span()
    {
        int fields = 0;
        foreach (string line in _lines)
        {
            fields++;
            foreach (char value in line.AsSpan()) if (value == ',') fields++;
        }
        return fields;
    }
}
