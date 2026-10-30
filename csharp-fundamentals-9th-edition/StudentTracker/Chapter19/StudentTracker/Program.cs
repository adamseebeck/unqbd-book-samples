using StudentTracker.Models;
using StudentTracker.Services;

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

    Console.Write($"Score (0 to {Student.MaxScore}): ");
    if (!int.TryParse(Console.ReadLine(), out int score))
    {
        Console.WriteLine("Type a whole number.");
        continue;
    }

    try
    {
        if (!roster.Add(new Student(name, score)))
        {
            Console.WriteLine($"{name} is already on the list.");
        }
    }
    catch (InvalidScoreException ex)
    {
        Console.WriteLine(ex.Message);
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
