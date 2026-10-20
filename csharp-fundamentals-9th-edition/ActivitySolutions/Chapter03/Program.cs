Console.Write("Enter your name: ");
string? name = Console.ReadLine();
Console.Write("Enter your favorite color: ");
string? color = Console.ReadLine();

if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(color))
{
    Console.WriteLine("Enter both a name and a color.");
}
else
{
    Console.WriteLine($"Nice to meet you, {name.Trim()}! I like {color.Trim()} too.");
    if (string.Equals(color.Trim(), "blue", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("That's my favorite color too!");
    }
}
