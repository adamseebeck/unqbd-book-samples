using StudentTracker.Models;
using StudentTracker.Services;

const int MaxScore = 100;
Roster roster = new Roster();
roster.StudentAdded += student =>
    Console.WriteLine($"Added {student.Describe()}");

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

    if (!roster.Add(new Student { Name = name, Score = score }))
    {
        Console.WriteLine($"{name} is already on the list.");
    }
}

if (roster.Students.Count == 0)
{
    Console.WriteLine("No students entered.");
    return;
}

Console.WriteLine("Class list:");
foreach (Student student in roster.Students.OrderBy(s => s.Name))
{
    Console.WriteLine($"  {student.Describe()}");
}

ClassSummary summary = new ClassSummary(
    roster.Students.Count,
    Math.Round(roster.Students.Average(s => s.Score), 1),
    roster.Students.Max(s => s.Score));
Console.WriteLine(summary);

Console.WriteLine("Honor roll (90 and above):");
var honorRoll = roster.Students
    .Where(s => s.Score >= 90)
    .OrderByDescending(s => s.Score);
foreach (Student student in honorRoll)
{
    Console.WriteLine($"  {student.Name}");
}

Console.Write("Look up a student: ");
Student? found = roster.Find(Console.ReadLine());
if (found is null)
{
    Console.WriteLine("No student with that name.");
}
else
{
    Console.WriteLine(found.Describe());
}
