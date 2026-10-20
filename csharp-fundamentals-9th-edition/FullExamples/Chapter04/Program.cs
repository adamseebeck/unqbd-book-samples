const decimal SalesTax = 0.07m;
decimal price = 20.00m;
decimal total = price + (price * SalesTax);

Console.WriteLine($"Price: {price:C}");
Console.WriteLine($"Total with tax: {total:C}");
Console.WriteLine($"Integer division: {5 / 2}");
Console.WriteLine($"Floating-point division: {5.0 / 2}");
