using StudentTracker.Services;

namespace StudentTracker.Models;

public class Student : Person
{
    public const int MaxScore = 100;

    public Student(string name, int score)
    {
        if (score < 0 || score > MaxScore)
        {
            throw new InvalidScoreException(
                $"Score must be from 0 to {MaxScore}.");
        }
        Name = name;
        Score = score;
    }

    public int Id { get; set; }
    public int Score { get; private set; }

    public string GetLetterGrade()
    {
        return GradeScale.GetLetter(Score);
    }

    public override string Describe()
    {
        return $"{Name}: {Score} ({GetLetterGrade()})";
    }
}
