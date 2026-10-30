using StudentTracker.Models;

const int MaxScore = 100;
const int StudentCount = 3;
Student[] students = new Student[StudentCount];

for (int i = 0; i < StudentCount; i++)
{
    Console.Write($"Student {i + 1} name: ");
    string? name = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(name))
    {
        Console.WriteLine("You did not enter a name.");
        return;
    }

    Console.Write($"Score (0 to {MaxScore}): ");
    if (!int.TryParse(Console.ReadLine(), out int score)
        || score < 0 || score > MaxScore)
    {
        Console.WriteLine($"Type a number from 0 to {MaxScore}.");
        return;
    }

    students[i] = new Student { Name = name, Score = score };
}

double total = 0;
int highest = 0;
foreach (Student student in students)
{
    Console.WriteLine(student.Describe());
    total += student.Score;
    if (student.Score > highest) highest = student.Score;
}
ClassSummary summary = new ClassSummary(
    StudentCount, Math.Round(total / StudentCount, 1), highest);
Console.WriteLine(summary);
