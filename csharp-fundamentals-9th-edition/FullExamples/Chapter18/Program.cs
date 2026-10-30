using System.Globalization;

try
{
    int first = ReadInteger("First integer:");
    int second = ReadInteger("Second integer:");
    Console.WriteLine($"Result: {first / second}");
}
catch (FormatException)
{
    Console.WriteLine("That was not a valid integer.");
}
catch (OverflowException)
{
    Console.WriteLine("The integer was too large or too small.");
}
catch (DivideByZeroException)
{
    Console.WriteLine("You cannot divide by zero.");
}
finally
{
    Console.WriteLine("Operation complete.");
}

CheckAge(18);
try
{
    throw new InvalidScoreException("Score cannot exceed 100.");
}
catch (InvalidScoreException ex)
{
    Console.WriteLine(ex.Message);
}

int ReadInteger(string prompt)
{
    Console.WriteLine(prompt);
    string? input = Console.ReadLine();
    if (input is null)
        throw new FormatException("A number is required.");
    return int.Parse(input, CultureInfo.InvariantCulture);
}

void CheckAge(int age)
{
    if (age < 0)
        throw new ArgumentOutOfRangeException(
            nameof(age), "Age cannot be negative.");
    Console.WriteLine($"Age is {age}");
}

class InvalidScoreException : Exception
{
    public InvalidScoreException(string message)
        : base(message) { }
}
