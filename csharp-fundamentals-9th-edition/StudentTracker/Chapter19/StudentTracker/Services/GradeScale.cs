namespace StudentTracker.Services;

public static class GradeScale
{
    public static string GetLetter(int score)
    {
        return score switch
        {
            < 60 => "F",
            < 70 => "D",
            < 80 => "C",
            < 90 => "B",
            _ => "A"
        };
    }
}
