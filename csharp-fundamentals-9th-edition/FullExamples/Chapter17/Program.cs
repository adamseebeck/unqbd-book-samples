using System.Diagnostics;

int[] numbers = Enumerable.Range(1, 20).ToArray();
int[] squares = new int[numbers.Length];
ParallelOptions options = new() { MaxDegreeOfParallelism = 2 };
Parallel.For(0, numbers.Length, options, i =>
{
    squares[i] = numbers[i] * numbers[i];
});
Console.WriteLine($"Squares: {string.Join(", ", squares)}");

var plinqSquares = numbers.AsParallel().AsOrdered()
    .Select(n => n * n).ToArray();
Console.WriteLine(
    $"PLINQ agrees: {squares.SequenceEqual(plinqSquares)}");

const int count = 100_000;
double[] serial = new double[count];
double[] parallel = new double[count];
// Warm up the loop bodies and parallel infrastructure
// before timing.
Parallel.For(0, 100, i => parallel[i] = Math.Sqrt(i));
for (int i = 0; i < 100; i++)
    serial[i] = Math.Sqrt(i);

Stopwatch watch = Stopwatch.StartNew();
for (int i = 0; i < count; i++)
    serial[i] = Math.Sqrt(i);
watch.Stop();
long serialTicks = watch.ElapsedTicks;

watch.Restart();
Parallel.For(0, count, options, i => parallel[i] = Math.Sqrt(i));
watch.Stop();
long parallelTicks = watch.ElapsedTicks;

Console.WriteLine(
    $"Results agree: {serial.SequenceEqual(parallel)}");
Console.WriteLine($"Serial stopwatch ticks: {serialTicks}");
Console.WriteLine($"Parallel stopwatch ticks: {parallelTicks}");
