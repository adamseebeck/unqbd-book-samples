SayHello();
GreetUser("Maria");
int sum = Add(3, 5);
Console.WriteLine($"The sum is {sum}");
DisplayMessage();
DisplayMessage("Welcome back!");
ShowInfo(age: 30, name: "Adam");
CheckNumber(-1);
CheckNumber(4);
ProcessData();

void SayHello()
{
    Console.WriteLine("Hello from a method!");
}

void GreetUser(string name)
{
    Console.WriteLine($"Hello, {name}!");
}

int Add(int a, int b)
{
    return a + b;
}

void DisplayMessage(string message = "Hello, world!")
{
    Console.WriteLine(message);
}

void ShowInfo(string name, int age)
{
    Console.WriteLine($"{name} is {age} years old.");
}

void CheckNumber(int number)
{
    if (number < 0)
    {
        Console.WriteLine("Negative numbers are not allowed.");
        return;
    }
    Console.WriteLine($"You entered {number}");
}

void ProcessData()
{
    void Log(string message)
    {
        Console.WriteLine($"[LOG] {message}");
    }
    Log("Processing started");
    Log("Processing complete");
}
