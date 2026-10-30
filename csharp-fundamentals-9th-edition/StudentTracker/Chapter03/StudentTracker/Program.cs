Console.Write("Student name: ");
string? name = Console.ReadLine();

if (string.IsNullOrWhiteSpace(name))
{
    Console.WriteLine("You did not enter a name.");
}
else
{
    Console.WriteLine($"Tracking scores for {name}.");
}
