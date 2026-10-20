Book[] books =
{
    new Book("A C# Guide", "Maria", 2026),
    new Book("Loop Lessons", "David", 2025),
    new Book("Data Basics", "Adam", 2024)
};
foreach (Book book in books)
{
    Console.WriteLine($"{book.Title} by {book.Author} ({book.YearPublished})");
}
Book updated = books[0] with { YearPublished = 2027 };
Book equalCopy = new Book("A C# Guide", "Maria", 2026);
Console.WriteLine($"Original year: {books[0].YearPublished}");
Console.WriteLine($"Updated year: {updated.YearPublished}");
Console.WriteLine($"Equal copy: {books[0] == equalCopy}");
Console.WriteLine($"Equal after update: {books[0] == updated}");

public record Book(string Title, string Author, int YearPublished);
