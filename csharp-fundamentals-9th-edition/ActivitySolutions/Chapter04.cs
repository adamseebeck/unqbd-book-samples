// This Chapter 4 exercise assumes valid numeric text and a nonzero second value.
Console.Write("Enter the first number: ");
string? firstInput = Console.ReadLine();
if (string.IsNullOrWhiteSpace(firstInput))
{
    Console.WriteLine("Enter a numeric first value.");
    return;
}
double first = double.Parse(firstInput);
Console.Write("Enter the second number (not zero): ");
string? secondInput = Console.ReadLine();
if (string.IsNullOrWhiteSpace(secondInput))
{
    Console.WriteLine("Enter a numeric second value.");
    return;
}
double second = double.Parse(secondInput);
if (second == 0)
{
    Console.WriteLine("Use a nonzero second number.");
    return;
}

Console.WriteLine($"Sum: {first + second}");
Console.WriteLine($"Difference: {first - second}");
Console.WriteLine($"Product: {first * second}");
Console.WriteLine($"Quotient: {Math.Round(first / second, 2)}");
