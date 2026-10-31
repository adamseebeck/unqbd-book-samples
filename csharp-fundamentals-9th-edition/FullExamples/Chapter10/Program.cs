Book book1 = new Book("A C# Guide", "Maria", 39.99m);
Book book2 = new Book("A C# Guide", "Maria", 39.99m);
Console.WriteLine($"Equal books: {book1 == book2}");
Book discounted = book1 with { Price = 34.99m };
Console.WriteLine($"Original price: {book1.Price}");
Console.WriteLine($"Discounted price: {discounted.Price}");

Student student = new Student { Name = "Maria", Grade = 10 };
Console.WriteLine($"{student.Name}: grade {student.Grade}");

Point p1 = new Point { X = 5, Y = 10 };
Point p2 = p1;
p2.X = 20;
p1.Display();
p2.Display();

CoffeeSize order = CoffeeSize.Medium;
Console.WriteLine($"Size: {order} ({(int)order})");

public record Book(string Title, string Author, decimal Price);

public record Student
{
    public string Name { get; init; } = "";
    public int Grade { get; init; }
}

public struct Point
{
    public int X;
    public int Y;
    public void Display()
    {
        Console.WriteLine($"({X}, {Y})");
    }
}

enum CoffeeSize
{
    Small,
    Medium,
    Large
}
