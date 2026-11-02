Person first = new() { Name = "Maria", Age = 30 };
Person second = first;
second.Age = 31;
first.Introduce();

class Person
{
    public string Name = "";
    public int Age;
    public void Introduce()
    {
        Console.WriteLine($"Hi, my name is {Name} and I am {Age} years old.");
    }
}
