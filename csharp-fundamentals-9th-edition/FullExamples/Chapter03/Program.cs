Console.Write("Enter your name: ");
string? name = Console.ReadLine();

if (string.IsNullOrWhiteSpace(name))
{
    Console.WriteLine("You did not enter a name.");
}
else
{
    Console.WriteLine($"Nice to meet you, {name}!");
}
