using System.Globalization;

Console.WriteLine("Enter integers separated by commas:");
string? input = Console.ReadLine();
if (string.IsNullOrWhiteSpace(input))
{
    Console.WriteLine("No numbers entered.");
    return;
}
List<int> numbers = new();
foreach (string part in input.Split(','))
{
    if (!int.TryParse(part.Trim(), out int value))
    {
        Console.WriteLine($"Invalid integer: {part.Trim()}");
        return;
    }
    numbers.Add(value);
}
List<int> filtered = numbers.Where(n => n > 10).OrderBy(n => n).ToList();
foreach (string line in filtered.Select(n => $"Number: {n}"))
    Console.WriteLine(line);
Console.WriteLine($"Count: {filtered.Count}");
Console.WriteLine(filtered.Count == 0
    ? "Average: no matching numbers"
    : $"Average: {filtered.Average().ToString("0.00", CultureInfo.InvariantCulture)}");
