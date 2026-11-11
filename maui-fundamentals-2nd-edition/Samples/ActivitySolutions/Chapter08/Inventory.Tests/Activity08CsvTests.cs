using MauiInventory.Core;
using Xunit;

namespace Inventory.Tests;
public sealed class Activity08CsvTests
{
    [Fact]
    public void NameLength_column_retains_exact_CSV_and_escaping()
    {
        var text = CategoryExport.ToCsv([new() { Id = 1, Name = "A", Description = "D" }]);
        Assert.Equal("CategoryId,Name,Description,NameLength\r\n1,\"A\",\"D\",1\r\n", text);
        var escaped = CategoryExport.ToCsv([new() { Id = 2, Name = "=1+1", Description = "One, \"two\"\nthree" }]);
        Assert.Contains("\"'=1+1\"", escaped);
        Assert.Contains("\"One, \"\"two\"\"\nthree\"", escaped);
        Assert.EndsWith(",4\r\n", escaped);
    }
}
