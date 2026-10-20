bool deterministic = args.Contains("--deterministic");
bool fail = args.Contains("--fail");
using CancellationTokenSource cancellation = new();
if (args.Contains("--cancel"))
    cancellation.Cancel();
Task<int>[] tasks =
{
    FetchDataAsync(1, cancellation.Token),
    FetchDataAsync(2, cancellation.Token),
    FetchDataAsync(3, cancellation.Token)
};
try
{
    int[] results = await Task.WhenAll(tasks);
    Console.WriteLine($"Results: {string.Join(", ", results)}");
    Console.WriteLine($"Sum: {results.Sum()}");
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.WriteLine("Fetch canceled.");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Fetch failed: {ex.Message}");
}

async Task<int> FetchDataAsync(int id, CancellationToken cancellationToken)
{
    await Task.Delay(20, cancellationToken);
    if (fail && id == 2)
        throw new InvalidOperationException("Request 2 failed.");
    return deterministic ? id * 10 : Random.Shared.Next(0, 100);
}
