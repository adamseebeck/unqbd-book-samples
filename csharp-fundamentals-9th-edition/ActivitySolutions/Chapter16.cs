using System.Globalization;

try
{
    int first = ReadInteger("First integer:");
    int second = ReadInteger("Second integer:");
    if (first < 0 || second < 0)
        throw new NegativeNumberException("Negative numbers are not allowed in this exercise.");
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
catch (NegativeNumberException ex)
{
    Console.WriteLine(ex.Message);
}
finally
{
    Console.WriteLine("Operation complete.");
}

int ReadInteger(string prompt)
{
    Console.WriteLine(prompt);
    string? input = Console.ReadLine();
    if (input is null)
        throw new FormatException("A number is required.");
    return int.Parse(input, CultureInfo.InvariantCulture);
}

class NegativeNumberException : Exception
{
    public NegativeNumberException(string message) : base(message) { }
}
