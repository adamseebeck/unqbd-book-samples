List<string> names = new List<string>();
names.Add("Alice");
names.Add("Bob");
names.Add("Charlie");
foreach (string name in names) Console.WriteLine(name);

DisplayItem(42);
DisplayItem("Hello");
Box<int> numberBox = new Box<int>(0);
numberBox.Pack(123);
Console.WriteLine(numberBox.Unpack());
Box<string> wordBox = new Box<string>("");
wordBox.Pack("Generics preserve types.");
Console.WriteLine(wordBox.Unpack());

Repository<int> repository = new Repository<int>();
Console.WriteLine($"Created integer: {repository.CreateInstance()}");

void DisplayItem<T>(T item)
{
    Console.WriteLine($"Item: {item}");
}

class Box<T>
{
    private T content;
    public Box(T initialValue)
    {
        content = initialValue;
    }
    public void Pack(T item)
    {
        content = item;
    }
    public T Unpack()
    {
        return content;
    }
}

class Repository<T> where T : new()
{
    public T CreateInstance()
    {
        return new T();
    }
}

class DataManager<T> where T : class, IDisposable, new()
{
}
