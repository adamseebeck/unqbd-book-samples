using SimpleCalculator;
using Xunit;

namespace SimpleCalculator.Tests;

public class CalculatorTests
{
    [Fact]
    public void Add_ReturnsCorrectSum()
    {
        int result = Calculator.Add(3, 4);
        Assert.Equal(7, result);
    }

    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(0, 0, 0)]
    [InlineData(-1, 1, 0)]
    public void Add_WorksWithVariousInputs(
        int a, int b, int expected)
    {
        Assert.Equal(expected, Calculator.Add(a, b));
    }

    [Fact]
    public void Subtract_ReturnsDifference()
        => Assert.Equal(5, Calculator.Subtract(9, 4));

    [Fact]
    public void Multiply_ReturnsProduct()
        => Assert.Equal(12, Calculator.Multiply(3, 4));

    [Fact]
    public void Divide_UsesIntegerDivision()
        => Assert.Equal(2, Calculator.Divide(10, 4));

    [Fact]
    public void Divide_ByZero_ThrowsException()
        => Assert.Throws<DivideByZeroException>(
            () => Calculator.Divide(10, 0));

    [Fact]
    public async Task GetScoreAsync_ReturnsExpectedValue()
    {
        ScoreSource source = new();
        int result = await source.GetScoreAsync(
            "Maria", TestContext.Current.CancellationToken);
        Assert.Equal(95, result);
    }

    [Fact]
    public async Task GetScoreAsync_MissingStudent_Throws()
    {
        ScoreSource source = new();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => source.GetScoreAsync("Missing",
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetScoreAsync_CanceledToken_Throws()
    {
        ScoreSource source = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.GetScoreAsync("Maria",
                cancellation.Token));
    }
}
