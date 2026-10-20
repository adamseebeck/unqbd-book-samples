Person person = new Person("Maria", 30);
person.Introduce();

Car car1 = new Car { Make = "Toyota", Model = "Camry", Year = 2024 };
car1.DisplayInfo();
Car car2 = new Car { Make = "Ford", Model = "Mustang", Year = 2023 };
car2.DisplayInfo();

Account account = new Account();
account.Deposit(25m);
Console.WriteLine($"Balance: {account.GetBalance()}");

class Person
{
    public string Name;
    public int Age;
    public Person(string name, int age)
    {
        Name = name;
        Age = age;
    }
    public void Introduce()
    {
        Console.WriteLine($"Hi, my name is {Name} and I am {Age} years old.");
    }
}

class Car
{
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public int Year { get; set; }
    public void DisplayInfo()
    {
        Console.WriteLine($"{Year} {Make} {Model}");
    }
}

class Account
{
    private decimal balance;
    public void Deposit(decimal amount)
    {
        if (amount > 0) balance += amount;
    }
    public decimal GetBalance()
    {
        return balance;
    }
}
