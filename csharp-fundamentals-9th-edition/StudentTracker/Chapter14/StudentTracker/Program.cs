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

double total = 0;
int highest = 0;
foreach (Student student in roster.Students)
{
    total += student.Score;
    if (student.Score > highest) highest = student.Score;
}
ClassSummary summary = new ClassSummary(roster.Students.Count,
    Math.Round(total / roster.Students.Count, 1), highest);
Console.WriteLine(summary);

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
