int[] numbers = { 1, 2, 3, 4, 5 };
Console.WriteLine($"First: {numbers[0]}");
foreach (int number in numbers)
    Console.WriteLine(number);

List<string> names = new();
names.Add("Alice");
names.Add("Bob");
names.Add("Charlie");
foreach (string name in names)
    Console.WriteLine(name);
names.Remove("Bob");
Console.WriteLine($"Count: {names.Count}");

Dictionary<string, string> capitals = new();
capitals["France"] = "Paris";
capitals["Japan"] = "Tokyo";
Console.WriteLine(capitals["France"]);
if (capitals.TryGetValue("Japan", out string? capital))
    Console.WriteLine($"Japan's capital is {capital}");

Queue<string> tasks = new();
tasks.Enqueue("Clean");
tasks.Enqueue("Cook");
tasks.Enqueue("Shop");
if (tasks.TryDequeue(out string? task))
    Console.WriteLine(task);

Stack<string> history = new();
history.Push("Page 1");
history.Push("Page 2");
history.Push("Page 3");
if (history.TryPop(out string? page))
    Console.WriteLine(page);

string[] values = { "Hello", "HELLO" };
List<string> presized =
    [with(capacity: values.Length * 2), .. values];
HashSet<string> unique =
    [with(StringComparer.OrdinalIgnoreCase), .. values];
Console.WriteLine($"Presized count: {presized.Count}");
Console.WriteLine($"Unique count: {unique.Count}");
