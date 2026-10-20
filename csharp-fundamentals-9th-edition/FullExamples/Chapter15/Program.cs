using System.Globalization;

int[] numbers = { 1, 2, 3, 4, 5, 6 };
var evenNumbers = numbers.Where(n => n % 2 == 0);
Console.WriteLine($"Even: {string.Join(", ", evenNumbers)}");

var methodResult = numbers.Where(n => n > 3)
    .Select(n => n * 10).OrderBy(n => n);
var queryResult = from n in numbers
                  where n > 3
                  orderby n
                  select n * 10;
Console.WriteLine($"Method: {string.Join(", ", methodResult)}");
Console.WriteLine($"Query: {string.Join(", ", queryResult)}");

string[] names = { "Anna", "Ben", "Clara", "David" };
Console.WriteLine($"Short: {string.Join(", ", names.Where(n => n.Length <= 4))}");
Console.WriteLine($"Upper: {string.Join(", ", names.Select(n => n.ToUpperInvariant()))}");

int[] values = { 10, 4, 7, 2, 9 };
Console.WriteLine($"Sorted: {string.Join(", ", values.OrderBy(v => v))}");
Console.WriteLine($"Max: {values.Max()}, Min: {values.Min()}, Average: {values.Average().ToString("0.0", CultureInfo.InvariantCulture)}");

List<int> data = new() { 1, 2, 3 };
var query = data.Where(x => x > 1);
List<int> snapshot = query.ToList();
data.Add(4);
Console.WriteLine($"Deferred: {string.Join(", ", query)}");
Console.WriteLine($"Snapshot: {string.Join(", ", snapshot)}");

var squares = numbers.Where(n => n % 2 == 0)
    .Select(n => n * n).OrderByDescending(n => n);
Console.WriteLine($"Squares: {string.Join(", ", squares)}");
