MessageHandler showMessage = ShowMessage;
showMessage("Hello from a delegate!");

Action<string> log = message => Console.WriteLine($"Log: {message}");
Func<int, int> square = x => x * x;
Predicate<int> isEven = x => x % 2 == 0;
log("Delegates make code flexible!");
Console.WriteLine(square(4));
Console.WriteLine(isEven(10));

Func<int, int, int> add = (a, b) => Add(a, b);
Func<int, int, int> multiply = (a, b) =>
{
    int product = a * b;
    return product;
};
Console.WriteLine(add(5, 7));
Console.WriteLine(multiply(3, 4));

Alarm alarm = new();
Action wakeUp = () => Console.WriteLine("Wake up!");
Action coffee = () => Console.WriteLine("Check the coffee pot.");
alarm.OnRing += wakeUp;
alarm.OnRing += coffee;
alarm.Ring();
alarm.OnRing -= coffee;
alarm.Ring();

void ShowMessage(string text) => Console.WriteLine(text);
int Add(int a, int b) => a + b;

delegate void MessageHandler(string message);

class Alarm
{
    public event Action? OnRing;
    public void Ring()
    {
        Console.WriteLine("Alarm ringing!");
        OnRing?.Invoke();
    }
}
