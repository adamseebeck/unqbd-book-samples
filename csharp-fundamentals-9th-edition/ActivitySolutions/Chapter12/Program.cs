Box<int> numberBox = new Box<int>(0);
numberBox.Pack(123);
Console.WriteLine(numberBox.Unpack());
numberBox.DisplayType();

Box<string> wordBox = new Box<string>("");
wordBox.Pack("Typed value");
Console.WriteLine(wordBox.Unpack());
wordBox.DisplayType();

ReferenceBox<string> referenceBox = new ReferenceBox<string>("Reference value");
Console.WriteLine(referenceBox.Unpack());
Box<object> objectBox = new Box<object>("A string inside an object box");
objectBox.DisplayType();
Console.WriteLine(objectBox.Unpack().GetType());
// ReferenceBox<int> is a deliberately invalid separate constraint experiment.

class Box<T>
{
    private T content;
    public Box(T initialValue) { content = initialValue; }
    public void Pack(T item) { content = item; }
    public T Unpack() { return content; }
    public void DisplayType() { Console.WriteLine(typeof(T)); }
}

class ReferenceBox<T> where T : class
{
    private T content;
    public ReferenceBox(T initialValue) { content = initialValue; }
    public void Pack(T item) { content = item; }
    public T Unpack() { return content; }
}
