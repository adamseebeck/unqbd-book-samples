string sentence = "Hello modern C#";
Console.WriteLine($"Capitalized: {sentence.IsCapitalized()}");
Console.WriteLine($"Words: {sentence.WordCount()}");
Console.WriteLine($"Question: {"Is it clear?".IsQuestion}");

DateTime monday = new(2026, 10, 12);
Console.WriteLine(
    $"Days until weekend: {monday.DaysUntilWeekend}");
Console.WriteLine($"Square: {int.Square(5)}");

IEnumerable<string> words = sentence.Split(
    ' ', StringSplitOptions.RemoveEmptyEntries);
Console.WriteLine($"Second word: {words[1]}");

public static class DemoExtensions
{
    public static bool IsCapitalized(this string? text) =>
        !string.IsNullOrEmpty(text) && char.IsUpper(text[0]);

    public static int WordCount(this string? text)
    {
        if (text is null)
        {
            return 0;
        }

        return text.Split(' ',
            StringSplitOptions.RemoveEmptyEntries).Length;
    }

    extension(string? text)
    {
        public bool IsQuestion =>
            text is not null && text.TrimEnd().EndsWith('?');
    }

    extension(DateTime date)
    {
        public int DaysUntilWeekend =>
            date.DayOfWeek is DayOfWeek.Saturday
                    or DayOfWeek.Sunday
                ? 0
                : (int)DayOfWeek.Saturday - (int)date.DayOfWeek;
    }

    extension(int)
    {
        public static int Square(int value)
            => checked(value * value);
    }
}

public static class SequenceExtensions
{
    extension<T>(IEnumerable<T> sequence)
    {
        public T this[int index] => sequence.ElementAt(index);
    }
}
