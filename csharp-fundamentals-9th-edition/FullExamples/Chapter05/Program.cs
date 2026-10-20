Console.Write("Enter a number between 1 and 3: ");
string? input = Console.ReadLine();
if (!int.TryParse(input, out int choice))
{
    Console.WriteLine("Enter a whole number.");
    return;
}

switch (choice)
{
    case 1:
        Console.WriteLine("You chose one.");
        break;
    case 2:
        Console.WriteLine("You chose two.");
        break;
    case 3:
        Console.WriteLine("You chose three.");
        break;
    default:
        Console.WriteLine("That's not a valid choice.");
        break;
}
