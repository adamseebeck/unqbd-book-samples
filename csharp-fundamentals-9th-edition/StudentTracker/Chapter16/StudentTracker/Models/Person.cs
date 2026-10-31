namespace StudentTracker.Models;

public class Person
{
    public string Name { get; set; } = "";

    public virtual string Describe()
    {
        return Name;
    }
}
