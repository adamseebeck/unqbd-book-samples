using System.Globalization;
using System.Text;

namespace AdventureWorks.Core;

// Chapter 17: checks a price feed of "sku,name,price" lines and
// counts its records. Strict UTF-8, at most 1,024 characters a
// line and 1,000 records; empty lines are skipped.
public static class PriceFeed
{
    public const int MaxLine = 1024, MaxRecords = 1000;
    static readonly UTF8Encoding Strict = new(false, true);

    public static async Task<int> CountAsync(Stream body,
        CancellationToken ct)
    {
        using var reader = new StreamReader(body, Strict, false);
        var records = 0;
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (line.Length == 0) continue;
            if (++records > MaxRecords)
                throw new FormatException("Over 1,000 records.");
            if (line.Length > MaxLine || !IsRecord(line))
                throw new FormatException(
                    $"Record {records} is not sku,name,price.");
        }
        return records;
    }

    // Three fields, no quotes, and a positive price, read through
    // spans over the line: no array and no string per field.
    public static bool IsRecord(ReadOnlySpan<char> line)
    {
        var first = line.IndexOf(',');
        var last = line.LastIndexOf(',');
        if (first <= 0 || last == first || line.Contains('"') ||
            line[(first + 1)..last].Contains(','))
            return false;
        return decimal.TryParse(line[(last + 1)..],
            NumberStyles.Number, CultureInfo.InvariantCulture,
            out var price) && price > 0;
    }
}
