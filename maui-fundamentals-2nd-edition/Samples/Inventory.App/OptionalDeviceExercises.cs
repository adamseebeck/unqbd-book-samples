namespace MauiInventory.App;
// Optional foreground exercises: invoke only from a user's explicit feature action.
public static class ContactExercise
{
    public static async Task<int?> CountAsync(CancellationToken token)
    {
        if (await Permissions.RequestAsync<Permissions.ContactsRead>() != PermissionStatus.Granted)
            return null;
        token.ThrowIfCancellationRequested();
        var contacts = await Contacts.Default.GetAllAsync(); // This API has no token overload.
        token.ThrowIfCancellationRequested();
        return contacts.Count(); // No names, telephone numbers or addresses are retained/logged.
    }
}

public sealed class OrientationExercise : IDisposable
{
    private bool compass, gyro;
    public double Heading { get; private set; }
    public double AngularSpeed { get; private set; }

    public void Start()
    {
        if (compass || gyro)
            return;
        try
        {
            if (Compass.Default.IsSupported && !Compass.Default.IsMonitoring)
            {
                Compass.Default.ReadingChanged += CompassReading;
                compass = true;
                Compass.Default.Start(SensorSpeed.UI);
            }

            if (Gyroscope.Default.IsSupported && !Gyroscope.Default.IsMonitoring)
            {
                Gyroscope.Default.ReadingChanged += GyroReading;
                gyro = true;
                Gyroscope.Default.Start(SensorSpeed.UI);
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private void CompassReading(object? sender, CompassChangedEventArgs args) => Heading = args.Reading.HeadingMagneticNorth;
    private void GyroReading(object? sender, GyroscopeChangedEventArgs args)
    {
        var v = args.Reading.AngularVelocity;
        AngularSpeed = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
    }

    public void Dispose()
    {
        if (compass)
        {
            Compass.Default.ReadingChanged -= CompassReading;
            Compass.Default.Stop();
            compass = false;
        }

        if (gyro)
        {
            Gyroscope.Default.ReadingChanged -= GyroReading;
            Gyroscope.Default.Stop();
            gyro = false;
        }
    }
}
