List<Student> students = new()
{
    new("Maria", 95), new("Ben", 87), new("Lila", 78),
    new("Jo", 60), new("Rae", 40)
};
if (args.Contains("--empty"))
    students.Clear();
if (args.Contains("--invalid"))
    students.Add(new("Invalid", 101));

foreach (Student student in students)
{
    if (student.Score is < 0 or > 100)
    {
        Console.WriteLine($"Invalid score for {student.Name}: {student.Score}");
        return;
    }
}
List<Student> selected = students.Where(s => s.Score >= 80)
    .OrderByDescending(s => s.Score).ThenBy(s => s.Name, StringComparer.Ordinal)
    .ToList();
if (selected.Count == 0)
    Console.WriteLine("No students scored 80 or above.");
foreach (string line in selected.Select(s => $"{s.Name} - {s.Score}"))
    Console.WriteLine(line);

foreach (var group in students.GroupBy(s => GetGrade(s.Score)).OrderBy(g => g.Key))
    Console.WriteLine($"Grade {group.Key}: {string.Join(", ", group.Select(s => s.Name))}");

string GetGrade(int score) => score switch
{
    >= 90 => "A",
    >= 80 => "B",
    >= 70 => "C",
    >= 60 => "D",
    _ => "F"
};

record Student(string Name, int Score);
