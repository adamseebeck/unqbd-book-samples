class Person
{
    public string Name { get; set; } = "";

    public virtual string Describe()
    {
        return Name;
    }
}
