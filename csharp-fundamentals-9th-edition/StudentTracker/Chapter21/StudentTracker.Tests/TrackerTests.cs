using StudentTracker.Models;
using StudentTracker.Services;
using Xunit;

namespace StudentTracker.Tests;

public class TrackerTests
{
    [Theory]
    [InlineData(100, "A")]
    [InlineData(90, "A")]
    [InlineData(89, "B")]
    [InlineData(60, "D")]
    [InlineData(59, "F")]
    public void GetLetter_UsesBoundaries(int score, string letter)
    {
        Assert.Equal(letter, GradeScale.GetLetter(score));
    }

    [Fact]
    public void Student_RejectsAScoreAbove100()
    {
        Assert.Throws<InvalidScoreException>(
            () => new Student("Maria", 101));
    }

    [Fact]
    public void Roster_RejectsTheSameNameInAnyCase()
    {
        Roster roster = new Roster();
        Assert.True(roster.Add(new Student("Maria", 92)));
        Assert.False(roster.Add(new Student("maria", 80)));
    }
}
