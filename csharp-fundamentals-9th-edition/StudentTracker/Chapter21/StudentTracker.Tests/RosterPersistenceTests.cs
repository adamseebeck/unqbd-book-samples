using System.Text.Json;
using StudentTracker.Models;
using StudentTracker.Services;
using Xunit;

namespace StudentTracker.Tests;

public class RosterPersistenceTests
{
    [Theory]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{\"Name\":null,\"Score\":92}]")]
    [InlineData("[{\"Name\":\"   \",\"Score\":92}]")]
    [InlineData("[{\"Name\":\"Maria\",\"Score\":101}]")]
    [InlineData("[{\"Name\":\"Maria\",\"Score\":-1}]")]
    [InlineData("[{\"Name\":\"Maria\",\"Score\":92},null]")]
    public void Load_RejectsInvalidDataWithoutChangingRoster(string json)
    {
        using StudentFile file = new(json);
        Roster roster = new();
        roster.Add(new Student("Existing", 70));

        Assert.Throws<JsonException>(() => roster.Load(file.Path));

        Student remaining = Assert.Single(roster.Students);
        Assert.Equal("Existing", remaining.Name);
        Assert.Equal(70, remaining.Score);
        Assert.Equal(json, File.ReadAllText(file.Path));
    }

    [Fact]
    public void SaveAndLoad_PreserveStudentValues()
    {
        using StudentFile file = new("[]");
        Roster original = new();
        original.Add(new Student("Maria", 92));
        original.Add(new Student("Ben", 85));
        original.Save(file.Path);

        Roster restored = new();
        restored.Load(file.Path);

        Assert.Equal(2, restored.Students.Count);
        Assert.Equal(92, restored.Find("maria")!.Score);
        Assert.Equal(85, restored.Find("Ben")!.Score);
    }

    private sealed class StudentFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"roster-test-{Guid.NewGuid():N}.json");

        public StudentFile(string json) => File.WriteAllText(Path, json);
        public void Dispose() => File.Delete(Path);
    }
}
