using System.Text.Json;
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

    public void Save(string path)
    {
        JsonSerializerOptions options =
            new() { WriteIndented = true };
        string json = JsonSerializer.Serialize(Students, options);
        File.WriteAllText(path, json);
    }

    public void Load(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }
        string json = File.ReadAllText(path);
        List<Student> saved =
            JsonSerializer.Deserialize<List<Student>>(json)
            ?? throw new JsonException("No student list found.");
        foreach (Student student in saved)
        {
            Add(student);
        }
    }
}
