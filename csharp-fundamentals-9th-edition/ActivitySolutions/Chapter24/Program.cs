Console.Write("Sentence: ");
string sentence = Console.ReadLine() ?? string.Empty;
Console.WriteLine($"Capitalized: {sentence.IsCapitalized()}");
Console.WriteLine($"Words: {sentence.WordCount()}");
Console.WriteLine($"Question: {sentence.IsQuestion}");

IEnumerable<string> words = sentence.Split(
    ' ', StringSplitOptions.RemoveEmptyEntries);
if (words.Any())
    Console.WriteLine($"First word: {words[0]}");

public static class StringExtensions
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
}

public static class SequenceExtensions
{
    extension<T>(IEnumerable<T> sequence)
    {
        public T this[int index] => sequence.ElementAt(index);
    }
}
