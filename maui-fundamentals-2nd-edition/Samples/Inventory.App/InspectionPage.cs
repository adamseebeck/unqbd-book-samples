using MauiInventory.Core;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MauiInventory.App;
public sealed class ShakeMonitor : IDisposable
{
    private readonly ShakeDetector detector = new();
    private readonly Stopwatch clock = new();
    private bool ownsMonitoring;
    public event EventHandler? ShakeDetected;
    public void Start()
    {
        if (ownsMonitoring)
            return;
        if (!Accelerometer.Default.IsSupported)
            throw new FeatureNotSupportedException();
        if (Accelerometer.Default.IsMonitoring)
            throw new InvalidOperationException("Another feature owns the accelerometer.");
        detector.Reset();
        clock.Restart();
        Accelerometer.Default.ReadingChanged += Reading;
        try
        {
            Accelerometer.Default.Start(SensorSpeed.Default);
            ownsMonitoring = true;
        }
        catch
        {
            Accelerometer.Default.ReadingChanged -= Reading;
            throw;
        }
    }

    private void Reading(object? sender, AccelerometerChangedEventArgs e)
    {
        var a = e.Reading.Acceleration;
        if (detector.Observe(a.X, a.Y, a.Z, clock.Elapsed))
            ShakeDetected?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        if (!ownsMonitoring)
            return;
        Accelerometer.Default.ReadingChanged -= Reading;
        Accelerometer.Default.Stop();
        ownsMonitoring = false;
        clock.Stop();
    }

    public void Dispose() => Stop();
}

public sealed class InspectionPage : ContentPage
{
    private readonly ShakeMonitor sensor = new();
    private readonly ObservableCollection<string> photos = [];
    private readonly Label status = new()
    {
        Text = "Start monitoring to enable the shake exercise."
    };
    private readonly ForegroundLocationMonitor location = new();
    private CancellationTokenSource? appearance;
    private bool active, confirming;
    private int generation;
    public InspectionPage()
    {
        Title = "Site inspection";
        var choose = new Button
        {
            Text = "Choose an exercise photo"
        };
        var capture = new Button
        {
            Text = "Capture an exercise photo"
        };
        var start = new Button
        {
            Text = "Start shake monitoring"
        };
        var clear = new Button
        {
            Text = "Clear exercise photo list"
        };
        choose.Clicked += async (_, _) => await AddPhotoAsync(false);
        capture.Clicked += async (_, _) => await AddPhotoAsync(true);
        var track = new Button
        {
            Text = "Start foreground location tracking"
        };
        track.Clicked += async (_, _) =>
        {
            if (appearance is null)
                return;
            var token = appearance.Token;
            try
            {
                await location.StartAsync(token);
                if (active && !token.IsCancellationRequested)
                    status.Text = location.Status;
            }
            catch (OperationCanceledException)when (token.IsCancellationRequested)
            {
            }
            catch (Exception)
            {
                if (active)
                    status.Text = "Location tracking is unavailable. Photo and clear controls remain usable.";
            }
        };
        start.Clicked += (_, _) =>
        {
            try
            {
                sensor.Start();
                status.Text = "Shake device to clear photos, or use the clear button.";
            }
            catch (Exception)
            {
                status.Text = "Accelerometer is unavailable or already in use.";
            }
        };
        clear.Clicked += async (_, _) => await ConfirmClearAsync();
        Content = new Grid
        {
            Padding = 16,
            RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star)],
            Children =
            {
                new VerticalStackLayout
                {
                    Children =
                    {
                        choose,
                        capture,
                        track,
                        start,
                        clear,
                        status
                    }
                },
                new CollectionView
                {
                    ItemsSource = photos
                }.AtRow(2)
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        appearance?.Dispose();
        appearance = new();
        active = true;
        generation++;
        sensor.ShakeDetected += OnShake;
        location.Updated += LocationUpdated;
    }

    protected override async void OnDisappearing()
    {
        active = false;
        generation++;
        appearance?.Cancel();
        sensor.ShakeDetected -= OnShake;
        location.Updated -= LocationUpdated;
        try
        {
            sensor.Stop();
            await location.StopAsync();
        }
        catch (Exception)
        { /* The feature has already left its active lifetime. */
        }

        base.OnDisappearing();
    }

    private void LocationUpdated(object? sender, EventArgs args) => MainThread.BeginInvokeOnMainThread(() =>
    {
        if (active)
            status.Text = location.Status;
    });
    private async Task AddPhotoAsync(bool capture)
    {
        if (appearance is null)
            return;
        var token = appearance.Token;
        var current = generation;
        try
        {
            if (capture && !MediaPicker.Default.IsCaptureSupported)
            {
                status.Text = "Camera capture is unavailable; choose a photo instead.";
                return;
            }

            var result = capture ? await MediaPicker.Default.CapturePhotoAsync() : (await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1 })).FirstOrDefault();
            token.ThrowIfCancellationRequested();
            if (result is null || !active || current != generation)
                return;
            // Only a display name is retained. The picker/camera owns its temporary file;
            // this exercise neither promises durable images nor deletes device photos.
            photos.Add(Path.GetFileName(result.FileName));
            status.Text = "Photo added to this temporary exercise list.";
        }
        catch (OperationCanceledException)when (token.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (active && current == generation)
                status.Text = "Photo selection or capture is unavailable or canceled.";
        }
    }

    private void OnShake(object? sender, EventArgs e)
    {
        var observed = generation;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (active && observed == generation)
                await ConfirmClearAsync();
        });
    }

    private async Task ConfirmClearAsync()
    {
        if (!active || confirming)
            return;
        confirming = true;
        var current = generation;
        try
        {
            if (await DisplayAlertAsync("Clear Photos?", "Clear the exercise photo list? Device photos will remain.", "Clear", "Keep") && active && current == generation)
            {
                photos.Clear();
                status.Text = "Exercise photo list cleared.";
            }
        }
        catch (Exception)
        {
            if (active)
                status.Text = "Could not show confirmation.";
        }
        finally
        {
            confirming = false;
        }
    }
}

internal static class GridPlacement
{
    public static T AtRow<T>(this T view, int row)
        where T : View
    {
        Grid.SetRow(view, row);
        return view;
    }
}
