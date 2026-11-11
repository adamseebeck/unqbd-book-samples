sealed class Faults
{
    public int Attempts;
    public int FailuresLeft;
    public int Status = StatusCodes.Status503ServiceUnavailable;
    public int DelayMs;
}
