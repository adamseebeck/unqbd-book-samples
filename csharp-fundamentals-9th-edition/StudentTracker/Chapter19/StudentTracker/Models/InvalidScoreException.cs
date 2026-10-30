namespace StudentTracker.Models;

public class InvalidScoreException : Exception
{
    public InvalidScoreException(string message)
        : base(message) { }
}
