using StudentTracker.Models;

namespace StudentTracker.Services;

public class Roster
{
    private Dictionary<string, Student> byName =
        new(StringComparer.OrdinalIgnoreCase);

    public List<Student> Students { get; } = new();

    public event Action<Student>? StudentAdded;

    public bool Add(Student student)
    {
        if (!byName.TryAdd(student.Name, student))
        {
            return false;
        }
        Students.Add(student);
        StudentAdded?.Invoke(student);
        return true;
    }

    public Student? Find(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        byName.TryGetValue(name, out Student? student);
        return student;
    }
}
