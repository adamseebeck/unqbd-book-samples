namespace SimpleCalculator;

public sealed class ScoreSource
{
    public async Task<int> GetScoreAsync(string name, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulated data retrieval
        return name == "Maria" ? 95
            : throw new KeyNotFoundException("Student was not found.");
    }
}
