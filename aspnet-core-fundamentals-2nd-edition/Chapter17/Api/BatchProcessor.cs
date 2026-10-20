using System.Buffers;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.ObjectPool;

public sealed record BatchResult(int Records, int Fields);
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(BatchResult))]
public partial class PerformanceJsonContext : JsonSerializerContext { }

public static class CsvParser
{
    public static int CountFields(ReadOnlySpan<char> line)
    {
        if (line.Contains('"')) throw new InvalidDataException("Quoted CSV is outside this lab's format.");
        int fields = 1;
        foreach (char value in line)
            if (value == ',') fields++;
        return fields;
    }
}
public sealed class BatchProcessor(ObjectPool<StringBuilder> lines)
{
    public async Task<BatchResult> ProcessAsync(Stream source, CancellationToken ct)
    {
        using var reader = new StreamReader(source, new UTF8Encoding(false, true),
            detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        var buffer = ArrayPool<char>.Shared.Rent(1024);
        var line = lines.Get();
        int records = 0, fields = 0;
        try
        {
            int read;
            while ((read = await reader.ReadAsync(buffer.AsMemory(0, 1024), ct)) != 0)
            {
                for (int i = 0; i < read; i++)
                {
                    if (buffer[i] == '\n') CompleteLine();
                    else
                    {
                        // Permit one terminal CR after 1024 content characters, pending LF or EOF.
                        if (line.Length >= 1024 && !(line.Length == 1024 && buffer[i] == '\r'))
                            throw new InvalidDataException("Line exceeds 1024 characters.");
                        line.Append(buffer[i]);
                    }
                }
            }
            if (line.Length > 0) CompleteLine();
            return new(records, fields);
        }
        finally
        {
            line.Clear();
            lines.Return(line);
            ArrayPool<char>.Shared.Return(buffer, clearArray: true);
        }

        void CompleteLine()
        {
            if (line.Length > 0 && line[^1] == '\r') line.Length--;
            if (line.Length > 0)
            {
                if (++records > 1000) throw new InvalidDataException("Batch exceeds 1000 records.");
                // One bounded string per record; this is not an allocation-free parser.
                fields += CsvParser.CountFields(line.ToString().AsSpan());
            }
            line.Clear();
        }
    }
}
public static class BufferExamples
{
    public static async Task<long> CountWithArrayPoolAsync(Stream source, CancellationToken ct)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            long total = 0;
            int read;
            while ((read = await source.ReadAsync(buffer.AsMemory(0, 8192), ct)) != 0) total += read;
            return total;
        }
        finally { ArrayPool<byte>.Shared.Return(buffer, clearArray: true); }
    }
    public static async Task<long> CountWithMemoryPoolAsync(Stream source, CancellationToken ct)
    {
        using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(8192);
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(owner.Memory[..8192], ct)) != 0) total += read;
        return total;
    }
    public static ValueTask<int> GetCachedLengthAsync(byte[]? cached,
        Func<Task<byte[]>> fetch) => cached is not null ? new(cached.Length) : new(FetchAsync(fetch));
    private static async Task<int> FetchAsync(Func<Task<byte[]>> fetch) => (await fetch()).Length;
}
