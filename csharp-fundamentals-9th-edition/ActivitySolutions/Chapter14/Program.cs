Alarm alarm = new();
Action wakeUp = () => Console.WriteLine("Wake up! Time to start the day.");
Action coffee = () => Console.WriteLine("Don't forget to check the coffee pot.");
alarm.OnRing += wakeUp;
alarm.OnRing += coffee;
alarm.Trigger();
alarm.OnRing -= coffee;
alarm.Trigger();

class Alarm
{
    public event Action? OnRing;
    public void Trigger()
    {
        Console.WriteLine("Alarm activated!");
        OnRing?.Invoke();
    }
}
