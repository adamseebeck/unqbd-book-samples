Config? config = null;
config?.Theme = "Dark";
Console.WriteLine($"Before initialization: {config is not null}");

config ??= new();
config?.Theme = "Dark";
config?.Language = "es";
config?.AutoSave = false;
Console.WriteLine($"{nameof(Config)} initialized: {config is not null}");
Console.WriteLine($"{nameof(Config.Theme)}: {config?.Theme}");
Console.WriteLine($"{nameof(Config.Language)}: {config?.Language}");
Console.WriteLine($"{nameof(Config.AutoSave)}: {config?.AutoSave}");
Console.WriteLine($"Generic name: {nameof(Dictionary<,>)}");

public class Config
{
    public string Theme
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? "Light" : value;
    } = "Light";

    public string Language { get; set; } = "en";
    public bool AutoSave { get; set; } = true;
}
