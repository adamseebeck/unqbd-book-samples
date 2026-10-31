using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

public static class CommandLog
{
    public static bool On { get; set; }
    public static int Count { get; set; }

    public static bool Filter(EventId id, LogLevel level) =>
        id.Id == RelationalEventId.CommandExecuted.Id;

    public static void Write(EventData data)
    {
        Count++;
        if (!On) return;
        var command = ((CommandExecutedEventData)data).Command;
        Console.WriteLine($"-- Command {Count}");
        Console.WriteLine(command.CommandText);
    }
}
