using StudentTracker.Models;

const int MaxScore = 100;
List<Student> students = new();
Dictionary<string, Student> byName =
    new(StringComparer.OrdinalIgnoreCase);

while (true)
{
    Console.Write("Student name (Enter to finish): ");
    string? name = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(name))
    {
        break;
    }

    Console.Write($"Score (0 to {MaxScore}): ");
    if (!int.TryParse(Console.ReadLine(), out int score)
        || score < 0 || score > MaxScore)
    {
        Console.WriteLine($"Type a number from 0 to {MaxScore}.");
        continue;
    }

    Student student = new Student { Name = name, Score = score };
    if (!byName.TryAdd(name, student))
    {
        Console.WriteLine($"{name} is already on the list.");
        continue;
    }
    students.Add(student);
}

if (students.Count == 0)
{
    Console.WriteLine("No students entered.");
    return;
}

double total = 0;
int highest = 0;
foreach (Student student in students)
{
    Console.WriteLine(student.Describe());
    total += student.Score;
    if (student.Score > highest) highest = student.Score;
}
ClassSummary summary = new ClassSummary(students.Count,
    Math.Round(total / students.Count, 1), highest);
Console.WriteLine(summary);

Console.Write("Look up a student: ");
string? search = Console.ReadLine();
if (!string.IsNullOrWhiteSpace(search)
    && byName.TryGetValue(search, out Student? found))
{
    Console.WriteLine(found.Describe());
}
else
{
    Console.WriteLine("No student with that name.");
}
