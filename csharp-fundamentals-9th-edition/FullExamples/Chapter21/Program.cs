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
{
    Console.WriteLine(
        $"Student: {student.Name} - {student.Score}");
}

var names = students.Select(s => s.Name);
Console.WriteLine($"Names: {string.Join(", ", names)}");

int total = students.Sum(s => s.Score);
double average = students.Average(s => s.Score);
int highest = students.Max(s => s.Score);
int lowest = students.Min(s => s.Score);

Console.WriteLine($"Total: {total}, Average: {average}, " +
    $"Max: {highest}, Min: {lowest}");

string[] animals = { "Dog", "Cat", "Deer", "Cow", "Dolphin" };

var groups = animals.GroupBy(a => a[0]);

foreach (var group in groups)
{
    Console.WriteLine(
        $"Starts with '{group.Key}': {string.Join(", ", group)}");
}

var gradeGroups = students
    .GroupBy(s => GetGrade(s.Score))
    .OrderBy(g => g.Key);

foreach (var group in gradeGroups)
{
    Console.WriteLine($"Grade {group.Key}: " +
        string.Join(", ", group.Select(s => s.Name)));
}

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
