const int MaxScore = 100;

Console.Write("Student name: ");
string? name = Console.ReadLine();
if (string.IsNullOrWhiteSpace(name))
{
    Console.WriteLine("You did not enter a name.");
    return;
}

Console.Write($"Score (0 to {MaxScore}): ");
int score = Convert.ToInt32(Console.ReadLine());
Console.WriteLine($"{name}: {score} out of {MaxScore}");
