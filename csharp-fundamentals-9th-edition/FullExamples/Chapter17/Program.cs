await DownloadMessageAsync();
Console.WriteLine("All done!");
int value = await GetDataAsync(CancellationToken.None);
Console.WriteLine($"The answer is {value}");

Task<int> t1 = GetDataAsync(CancellationToken.None);
Task<int> t2 = GetDataAsync(CancellationToken.None);
Task<int> t3 = GetDataAsync(CancellationToken.None);
int[] results = await Task.WhenAll(t1, t2, t3);
Console.WriteLine($"Sum: {results.Sum()}");

try
{
    await FailingOperationAsync();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

using CancellationTokenSource cancellation = new();
cancellation.Cancel();
try
{
    await GetDataAsync(cancellation.Token);
}
catch (OperationCanceledException)
    when (cancellation.IsCancellationRequested)
{
    Console.WriteLine("Operation canceled.");
}

int total = await Task.Run(() => Enumerable.Range(1, 5).Sum());
Console.WriteLine($"CPU total: {total}");

async Task DownloadMessageAsync()
{
    Console.WriteLine("Starting download...");
    await Task.Delay(20);
    Console.WriteLine("Download complete!");
}

async Task<int> GetDataAsync(CancellationToken cancellationToken)
{
    await Task.Delay(20, cancellationToken);
    return 42;
}

async Task FailingOperationAsync()
{
    await Task.Delay(20);
    throw new InvalidOperationException("Something went wrong");
}
