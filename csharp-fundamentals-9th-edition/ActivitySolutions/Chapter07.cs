Console.Write("First number: ");
if (!double.TryParse(Console.ReadLine(), out double first))
{
    Console.WriteLine("Enter a number.");
    return;
}
Console.Write("Second number: ");
if (!double.TryParse(Console.ReadLine(), out double second))
{
    Console.WriteLine("Enter a number.");
    return;
}
Console.Write("Operation (+, -, *, /): ");
string? operation = Console.ReadLine();
if (operation == "/" && second == 0)
{
    Console.WriteLine("Division by zero is not allowed.");
    return;
}
double result;
switch (operation)
{
    case "+": result = Add(first, second); break;
    case "-": result = Subtract(first, second); break;
    case "*": result = Multiply(first, second); break;
    case "/": result = Divide(first, second); break;
    default:
        Console.WriteLine("Unknown operation.");
        return;
}
Console.WriteLine($"Result: {result}");
Console.Write("Whole number for an even/odd check: ");
if (!int.TryParse(Console.ReadLine(), out int wholeNumber))
{
    Console.WriteLine("Enter a whole number.");
    return;
}
Console.WriteLine(IsEven(wholeNumber) ? "Even" : "Odd");

double Add(double a, double b) { return a + b; }
double Subtract(double a, double b) { return a - b; }
double Multiply(double a, double b) { return a * b; }
double Divide(double a, double b) { return a / b; }
bool IsEven(int number) { return number % 2 == 0; }
