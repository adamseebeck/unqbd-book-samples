using StudentTracker.Services;

namespace StudentTracker.Models;

public class Student : Person
{
    public int Score { get; set; }

    public string GetLetterGrade()
    {
        return GradeScale.GetLetter(Score);
    }

    public override string Describe()
    {
        return $"{Name}: {Score} ({GetLetterGrade()})";
    }
}
