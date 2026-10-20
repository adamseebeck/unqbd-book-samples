Dictionary<string, string> contacts = new(StringComparer.OrdinalIgnoreCase);
while (true)
{
    Console.WriteLine("1 Add, 2 View, 3 Search, 4 Remove, 5 Export, 0 Exit");
    string? choice = Console.ReadLine();
    if (choice is null or "0")
        break;
    if (choice == "2")
    {
        if (contacts.Count == 0)
            Console.WriteLine("No contacts.");
        foreach (var contact in contacts.OrderBy(c => c.Key, StringComparer.OrdinalIgnoreCase))
            Console.WriteLine($"{contact.Key}: {contact.Value}");
        continue;
    }
    if (choice == "5")
    {
        string[] lines = contacts
            .OrderBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
            .Select(c => $"{c.Key}: {c.Value}").ToArray();
        File.WriteAllLines("contacts-export.txt", lines);
        Console.WriteLine("Exported to contacts-export.txt.");
        continue;
    }
    if (choice is not ("1" or "3" or "4"))
    {
        Console.WriteLine("Unknown option.");
        continue;
    }
    Console.WriteLine("Name:");
    string? name = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(name))
    {
        Console.WriteLine("A name is required.");
        continue;
    }
    if (choice == "1")
    {
        Console.WriteLine("Phone:");
        string? phone = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(phone))
            Console.WriteLine("A phone number is required.");
        else if (!contacts.TryAdd(name, phone))
            Console.WriteLine("Contact already exists.");
        else
            Console.WriteLine("Contact added.");
    }
    else if (choice == "3")
        Console.WriteLine(contacts.TryGetValue(name, out string? phone)
            ? $"{name}: {phone}" : "Contact not found.");
    else
        Console.WriteLine(contacts.Remove(name) ? "Contact removed." : "Contact not found.");
}
