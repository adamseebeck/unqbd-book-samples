Sensor sensor = new("Desk", 50);
int notifications = 0;
EventHandler<ThresholdReachedEventArgs> handler = (_, data) =>
{
    notifications++;
    Console.WriteLine($"{data.SensorName}: {data.Reading} exceeded {data.Threshold}");
};

sensor.ThresholdReached += handler;
sensor.ReadValue(25);
sensor.ReadValue(72);
sensor.ThresholdReached -= handler;
sensor.ReadValue(88);
Console.WriteLine($"Notifications: {notifications}");

public partial class Sensor
{
    public partial Sensor(string name, int threshold);
    public partial event EventHandler<ThresholdReachedEventArgs>? ThresholdReached;
    public string Name { get; }
    public int Threshold { get; }
}

public partial class Sensor
{
    private EventHandler<ThresholdReachedEventArgs>? _thresholdReached;

    public partial Sensor(string name, int threshold)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Threshold = threshold;
        Console.WriteLine($"Sensor initialized: {Name}");
    }

    public partial event EventHandler<ThresholdReachedEventArgs>? ThresholdReached
    {
        add => _thresholdReached += value;
        remove => _thresholdReached -= value;
    }

    public void ReadValue(int value)
    {
        if (value > Threshold)
            _thresholdReached?.Invoke(this,
                new ThresholdReachedEventArgs(Name, value, Threshold));
    }
}

public sealed class ThresholdReachedEventArgs : EventArgs
{
    public string SensorName { get; }
    public int Reading { get; }
    public int Threshold { get; }

    public ThresholdReachedEventArgs(string sensorName, int reading, int threshold)
    {
        SensorName = sensorName;
        Reading = reading;
        Threshold = threshold;
    }
}
