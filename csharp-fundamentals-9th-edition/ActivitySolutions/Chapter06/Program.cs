bool runAgain = true;
while (runAgain)
{
    double[] numbers = new double[10];
    for (int i = 0; i < numbers.Length; i++)
    {
        while (true)
        {
            Console.WriteLine($"Enter number {i + 1}:");
            string? input = Console.ReadLine();
            if (input is null)
            {
                Console.WriteLine("Input ended before all ten numbers were read.");
                return;
            }
            if (double.TryParse(input, out double value))
            {
                numbers[i] = value;
                break;
            }
            Console.WriteLine("Enter a number.");
        }
    }

    double total = 0;
    double smallest = numbers[0];
    double largest = numbers[0];
    foreach (double value in numbers)
    {
        total += value;
        if (value < smallest) smallest = value;
        if (value > largest) largest = value;
    }
    double average = total / numbers.Length;
    Console.WriteLine($"Total: {total}");
    Console.WriteLine($"Average: {average}");
    Console.WriteLine($"Smallest: {smallest}");
    Console.WriteLine($"Largest: {largest}");
    Console.WriteLine("Numbers entered:");
    foreach (double value in numbers) Console.WriteLine(value);

    int aboveAverage = 0;
    foreach (double value in numbers)
    {
        if (value > average) aboveAverage++;
    }
    Console.WriteLine($"Above average: {aboveAverage}");

    Console.WriteLine("Numbers in reverse:");
    for (int i = numbers.Length - 1; i >= 0; i--)
    {
        Console.WriteLine(numbers[i]);
    }

    Console.WriteLine("Run again? Type yes to repeat or no to stop:");
    string? response = Console.ReadLine();
    runAgain = string.Equals(response?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);
}
