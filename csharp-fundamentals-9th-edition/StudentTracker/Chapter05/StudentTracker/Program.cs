const int MaxScore = 100;

Console.Write("Student name: ");
string? name = Console.ReadLine();
if (string.IsNullOrWhiteSpace(name))
{
    Console.WriteLine("You did not enter a name.");
    return;
}

Console.Write($"Score (0 to {MaxScore}): ");
if (!int.TryParse(Console.ReadLine(), out int score)
    || score < 0 || score > MaxScore)
{
    Console.WriteLine($"Type a number from 0 to {MaxScore}.");
    return;
}

string letter = score switch
{
    < 60 => "F",
    < 70 => "D",
    < 80 => "C",
    < 90 => "B",
    _ => "A"
};
Console.WriteLine($"{name}: {score} ({letter})");
