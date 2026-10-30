using System.Text.Json;

string directory = Path.Combine(
    Path.GetTempPath(), $"csharp-files-{Guid.NewGuid():N}");
Directory.CreateDirectory(directory);
string textPath = Path.Combine(directory, "example.txt");
File.WriteAllText(textPath, "Hello from C# 15 and .NET 11!");
Console.WriteLine(File.ReadAllText(textPath));
File.AppendAllText(
    textPath, Environment.NewLine + "Appended line of text.");
Console.WriteLine(
    $"Text lines: {File.ReadAllLines(textPath).Length}");

string binaryPath = Path.Combine(directory, "data.bin");
using (FileStream writer =
    new(binaryPath, FileMode.CreateNew, FileAccess.Write))
{
    byte[] bytes = { 1, 2, 3, 4, 5 };
    writer.Write(bytes);
}
byte[] buffer = new byte[5];
using (FileStream reader =
    new(binaryPath, FileMode.Open, FileAccess.Read))
{
    reader.ReadExactly(buffer);
}
Console.WriteLine($"Bytes: {string.Join(", ", buffer)}");

Person person = new() { Name = "Maria", Age = 30 };
string jsonPath = Path.Combine(directory, "person.json");
File.WriteAllText(jsonPath, JsonSerializer.Serialize(person));
Person restored = JsonSerializer.Deserialize<Person>(
    File.ReadAllText(jsonPath))
    ?? throw new JsonException(
        "The JSON did not contain a person.");
Console.WriteLine($"Name: {restored.Name}, Age: {restored.Age}");
JsonSerializerOptions options = new() { WriteIndented = true };
File.WriteAllText(Path.Combine(directory, "person_pretty.json"),
    JsonSerializer.Serialize(person, options));

try
{
    File.ReadAllText(Path.Combine(directory, "missing.txt"));
}
catch (FileNotFoundException)
{
    Console.WriteLine("File not found.");
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine("Access denied.");
}

class Person
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}
