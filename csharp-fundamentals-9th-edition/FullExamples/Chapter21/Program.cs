using System.Globalization;

List<int> numbers = new() { 3, 9, 2, 7, 6, 1 };
var evens = numbers.Where(n => n % 2 == 0).OrderBy(n => n);
Console.WriteLine($"Even: {string.Join(", ", evens)}");
string[] names = { "Maria", "Ben", "Lila" };
foreach (string message in names
             .Select(name => $"Hello, {name}!"))
    Console.WriteLine(message);

string[] animals = { "Dog", "Cat", "Deer", "Cow", "Dolphin" };
foreach (var group in animals.GroupBy(a => a[0]))
    Console.WriteLine(
        $"Starts with '{group.Key}': {string.Join(", ", group)}");

int[] scores = { 85, 92, 78, 90, 88 };
string average = scores.Average()
    .ToString("0.0", CultureInfo.InvariantCulture);
Console.WriteLine($"Total: {scores.Sum()}, Average: {average}, " +
    $"Max: {scores.Max()}, Min: {scores.Min()}");
foreach (string line in scores.Where(s => s >= 85)
             .OrderByDescending(s => s)
             .Select(s => $"High score: {s}"))
    Console.WriteLine(line);

List<int> values = new() { 1, 2, 3 };
var live = values.Where(v => v > 1);
values.Add(4);
Console.WriteLine($"Deferred: {string.Join(", ", live)}");

List<Student> students = new()
{
    new("Maria", 95), new("Ben", 87),
    new("Lila", 78), new("Jo", 60)
};
List<Student> passing = students.Where(s => s.Score >= 80)
    .OrderByDescending(s => s.Score)
    .ThenBy(s => s.Name, StringComparer.Ordinal)
    .ToList();
foreach (Student student in passing)
    Console.WriteLine(
        $"Student: {student.Name} - {student.Score}");
foreach (var group in students.GroupBy(s => GetGrade(s.Score))
             .OrderBy(g => g.Key))
    Console.WriteLine($"Grade {group.Key}: " +
        string.Join(", ", group.Select(s => s.Name)));

string GetGrade(int score) => score switch
{
    < 0 or > 100 =>
        throw new ArgumentOutOfRangeException(nameof(score)),
    >= 90 => "A",
    >= 80 => "B",
    >= 70 => "C",
    >= 60 => "D",
    _ => "F"
};

record Student(string Name, int Score);
