AppSettings? settings = default;
int calls = 0;

string BuildTheme()
{
    calls++;
    return "Dark";
}

settings?.Theme = BuildTheme();
Console.WriteLine($"Before initialization: calls={calls}");

settings ??= new();
settings?.Theme = BuildTheme();
settings?.Saves += 1;

string[]? labels = null;
labels?[0] = BuildTheme();
labels = ["Default"];
labels?[0] = "Saved";

Console.WriteLine($"After initialization: calls={calls}");
Console.WriteLine($"Theme: {settings?.Theme}");
Console.WriteLine($"Saves: {settings?.Saves}");
Console.WriteLine($"Label: {labels?[0]}");
Console.WriteLine($"Type: {nameof(AppSettings)}");
Console.WriteLine($"Property: {nameof(AppSettings.Theme)}");
Console.WriteLine($"Generic: {nameof(Dictionary<,>)}");

public class AppSettings
{
    public string Theme
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? "Light" : value;
    } = "Light";

    public int Saves { get; set; }
}
