namespace MauiInventory.App;
public partial class App : Application
{
    private readonly AppShell shell;
    public App(AppShell shell)
    {
        InitializeComponent();
        this.shell = shell;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(shell);
        // Persist small, non-sensitive state continuously. Stopped is a fallback only.
        window.Stopped += (_, _) => Preferences.Default.Set("last_stop_utc", DateTimeOffset.UtcNow.ToString("O"));
        return window;
    }
}
