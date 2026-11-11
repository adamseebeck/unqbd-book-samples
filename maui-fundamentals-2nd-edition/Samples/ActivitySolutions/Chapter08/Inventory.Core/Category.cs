using SQLite;
using System.Globalization;
using System.Text;

namespace MauiInventory.Core;
public sealed class Category
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    // A screen reader on Windows announces a list row by its item's text, so give a row its category's name.
    public override string ToString() => Name;
}

public interface ITextFileStore
{
    Task WriteAsync(string name, string contents, CancellationToken token);
}

public static class CategoryExport
{
    private static string Cell(string value)
    {
        // Quotes preserve CSV syntax. A leading apostrophe separately prevents spreadsheet
        // programs interpreting untrusted names as formulas when the exported file is opened.
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@'))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    public static string ToCsv(IEnumerable<Category> rows)
    {
        var csv = new StringBuilder("CategoryId,Name,Description,NameLength\r\n");
        foreach (var row in rows)
            csv.Append(row.Id.ToString(CultureInfo.InvariantCulture)).Append(',').Append(Cell(row.Name)).Append(',').Append(Cell(row.Description)).Append(',').Append(row.Name.Length.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
        return csv.ToString();
    }

    public static async Task<DateTimeOffset> ExportAsync(IEnumerable<Category> rows, ITextFileStore files, CancellationToken token = default)
    {
        await files.WriteAsync("categories.csv", ToCsv(rows), token);
        return DateTimeOffset.UtcNow; // Caller updates LastExportUtc only after successful write.
    }
}
