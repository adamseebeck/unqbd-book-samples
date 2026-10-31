using System.Diagnostics;

int[] numbers = Enumerable.Range(1, 20).ToArray();
Parallel.ForEach(numbers, n =>
{
    Console.WriteLine($"Square {n}: {n * n}; thread {Environment.CurrentManagedThreadId}");
});
Console.WriteLine("Serial order:");
foreach (int n in numbers)
    Console.WriteLine($"Square {n}: {n * n}");

if (args.Contains("--benchmark"))
{
    const int count = 10_000_000;
    double[] serial = new double[count];
    double[] parallel = new double[count];
    Action<int> squareRoot = i => parallel[i] = Math.Sqrt(i);

    // Warm up both loops with the full count before timing.
    for (int i = 0; i < count; i++)
        serial[i] = Math.Sqrt(i);
    Parallel.For(0, count, squareRoot);

    Stopwatch watch = Stopwatch.StartNew();
    for (int i = 0; i < count; i++)
        serial[i] = Math.Sqrt(i);
    watch.Stop();
    Console.WriteLine($"Serial milliseconds: {watch.ElapsedMilliseconds}");

    watch.Restart();
    Parallel.For(0, count, squareRoot);
    watch.Stop();
    Console.WriteLine($"Parallel milliseconds: {watch.ElapsedMilliseconds}");
    Console.WriteLine($"Results agree: {serial.SequenceEqual(parallel)}");
}
