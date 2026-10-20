using System.Text;
using System.Text.Json;
using Microsoft.Extensions.ObjectPool;

int passed = 0;
void Check(string name, bool result)
{
    if (!result) throw new Exception(name);
    passed++;
    Console.WriteLine($"PASS {name}");
}
var pool = new DefaultObjectPoolProvider().CreateStringBuilderPool(1024, 2048);
var processor = new BatchProcessor(pool);
await using var fragmented = new FragmentedStream(Encoding.UTF8.GetBytes("é,猫,\r\na,b,c"));
var result = await processor.ProcessAsync(fragmented, CancellationToken.None);
Check("fragmented UTF8, CRLF, final unterminated line and trailing field", result == new BatchResult(2, 6));
Check("generated JSON roundtrip", JsonSerializer.Deserialize(JsonSerializer.Serialize(result,
    PerformanceJsonContext.Default.BatchResult), PerformanceJsonContext.Default.BatchResult) == result);
Check("trailing empty field", CsvParser.CountFields("a,b,".AsSpan()) == 3);
foreach (int length in new[] { 1023, 1024, 1025 })
foreach (string ending in new[] { "\n", "\r\n" })
{
    var name = $"line boundary {length} ending {(ending.Length == 2 ? "CRLF" : "LF")}";
    try
    {
        await processor.ProcessAsync(new MemoryStream(Encoding.UTF8.GetBytes(new string('a', length) + ending)), CancellationToken.None);
        Check(name, length <= 1024);
    }
    catch (InvalidDataException) { Check(name, length > 1024); }
}
foreach (var (name, bytes) in new (string, byte[])[]
{
    ("oversized line", Encoding.UTF8.GetBytes(new string('x', 1025))),
    ("too many records", Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("a\n", 1001)))),
    ("quoted format rejected", Encoding.UTF8.GetBytes("\"a,b\",c\n")),
    ("incomplete UTF8 rejected", [0x61, 0xC3])
})
{
    try { await processor.ProcessAsync(new MemoryStream(bytes), CancellationToken.None); Check(name, false); }
    catch (Exception ex) when (ex is InvalidDataException or DecoderFallbackException) { Check(name, true); }
}
using var cancelled = new CancellationTokenSource();
cancelled.Cancel();
try { await processor.ProcessAsync(new MemoryStream([1]), cancelled.Token); Check("cancellation", false); }
catch (OperationCanceledException) { Check("cancellation", true); }
var afterFailure = await processor.ProcessAsync(new MemoryStream(Encoding.UTF8.GetBytes("a,b\n")), CancellationToken.None);
Check("pooled state cleared after failures", afterFailure == new BatchResult(1, 2));
Check("array pool count", await BufferExamples.CountWithArrayPoolAsync(new MemoryStream(new byte[9000]), CancellationToken.None) == 9000);
Check("memory pool count", await BufferExamples.CountWithMemoryPoolAsync(new MemoryStream(new byte[9000]), CancellationToken.None) == 9000);
int fetches = 0;
Task<byte[]> Fetch() { fetches++; return Task.FromResult(new byte[7]); }
Check("ValueTask cached and fetched paths", await BufferExamples.GetCachedLengthAsync(new byte[3], Fetch) == 3 &&
    await BufferExamples.GetCachedLengthAsync(null, Fetch) == 7 && fetches == 1);
var benchmark = new CsvParsingBenchmarks();
Check("benchmark methods equivalent", benchmark.Split() == 3000 && benchmark.Span() == 3000);
Console.WriteLine($"TOTAL {passed} passed");

sealed class FragmentedStream(byte[] bytes) : MemoryStream(bytes)
{
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        base.ReadAsync(buffer[..Math.Min(buffer.Length, 1)], cancellationToken);
}
