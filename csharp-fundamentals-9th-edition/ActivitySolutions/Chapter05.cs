Console.Write("Temperature in degrees Fahrenheit: ");
if (!double.TryParse(Console.ReadLine(), out double temperature)
    || !double.IsFinite(temperature))
{
    Console.WriteLine("Enter a finite numeric temperature.");
    return;
}

Console.Write("Humidity percentage (0-100): ");
if (!int.TryParse(Console.ReadLine(), out int humidity)
    || humidity < 0 || humidity > 100)
{
    Console.WriteLine("Enter a whole-number humidity from 0 to 100.");
    return;
}

string message = temperature switch
{
    < 50 => "It's cold outside.",
    < 85 => "It's warm outside.",
    _ => "It's very hot, stay hydrated."
};
Console.WriteLine(message);

if (temperature >= 80 && humidity >= 70)
{
    Console.WriteLine("High temperature and humidity increase heat discomfort.");
}
