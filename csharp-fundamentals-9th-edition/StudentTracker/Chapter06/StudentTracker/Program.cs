const int MaxScore = 100;
const int StudentCount = 3;
string[] names = new string[StudentCount];
int[] scores = new int[StudentCount];

for (int i = 0; i < StudentCount; i++)
{
    Console.Write($"Student {i + 1} name: ");
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

    names[i] = name;
    scores[i] = score;
}

double total = 0;
for (int i = 0; i < StudentCount; i++)
{
    string letter = scores[i] switch
    {
        < 60 => "F",
        < 70 => "D",
        < 80 => "C",
        < 90 => "B",
        _ => "A"
    };
    Console.WriteLine($"{names[i]}: {scores[i]} ({letter})");
    total += scores[i];
}
double average = Math.Round(total / StudentCount, 1);
Console.WriteLine($"Class average: {average}");
