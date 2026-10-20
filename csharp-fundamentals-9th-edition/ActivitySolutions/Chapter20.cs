using System.Text.Json;

string directory = Path.Combine(Path.GetTempPath(), $"csharp-contacts-{Guid.NewGuid():N}");
Directory.CreateDirectory(directory);
string path = Path.Combine(directory, "contacts.json");
List<Contact> originals = new()
{
    new() { Name = "Maria", Email = "maria@example.test", Phone = "555-0100" },
    new() { Name = "Ben", Email = "ben@example.test", Phone = "555-0101" },
    new() { Name = "Lila", Email = "lila@example.test", Phone = "555-0102" }
};
if (args.Contains("--corrupt"))
    File.WriteAllText(path, "{ not valid JSON");
else if (!args.Contains("--missing"))
    File.WriteAllText(path, JsonSerializer.Serialize(originals));

List<Contact> contacts;
try
{
    contacts = JsonSerializer.Deserialize<List<Contact>>(File.ReadAllText(path))
        ?? throw new JsonException("A contact list is required.");
}
catch (FileNotFoundException)
{
    Console.WriteLine("No saved contacts.");
    contacts = new();
}
catch (JsonException)
{
    Console.WriteLine("Saved contacts contain invalid JSON.");
    contacts = new();
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine("Saved contacts cannot be accessed.");
    contacts = new();
}
catch (IOException)
{
    Console.WriteLine("Saved contacts could not be read.");
    contacts = new();
}
foreach (Contact contact in contacts)
    Console.WriteLine($"{contact.Name}: {contact.Email}, {contact.Phone}");
Console.WriteLine($"Count: {contacts.Count}");

class Contact
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
