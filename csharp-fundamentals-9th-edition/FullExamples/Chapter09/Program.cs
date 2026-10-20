Animal[] animals =
{
    new Dog { Name = "Buddy" },
    new Cat { Name = "Luna" },
    new Bird { Name = "Tweety" }
};
foreach (Animal animal in animals) animal.Speak();

Bird bird = new Bird { Name = "Robin" };
bird.Eat();
Animal baseReference = bird;
baseReference.Eat();

class Animal
{
    public string Name { get; set; } = "";
    public void Eat()
    {
        Console.WriteLine($"{Name} is eating.");
    }
    public virtual void Speak()
    {
        Console.WriteLine($"{Name} makes a sound.");
    }
}

class Dog : Animal
{
    public void Bark()
    {
        Console.WriteLine($"{Name} says: Woof!");
    }
    public override void Speak()
    {
        Bark();
    }
}

class Cat : Animal
{
    public override void Speak()
    {
        Console.WriteLine($"{Name} says: Meow!");
    }
}

class Bird : Animal
{
    public override void Speak()
    {
        Console.WriteLine($"{Name} says: Tweet!");
    }
    public new void Eat()
    {
        Console.WriteLine($"{Name} pecks at food.");
    }
}
