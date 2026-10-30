namespace SimpleCalculator;

public sealed class ScoreSource
{
    public async Task<int> GetScoreAsync(string name,
        CancellationToken cancellationToken = default)
    {
        // Simulated data retrieval
        await Task.Delay(10, cancellationToken);
        return name == "Maria" ? 95
            : throw new KeyNotFoundException(
                "Student was not found.");
    }
}
