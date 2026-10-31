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

Shape[] shapes =
{
    new Rectangle { Name = "Garden", Width = 3, Height = 4 },
    new Triangle { Name = "Sail", Width = 6, Height = 5 }
};
foreach (Shape shape in shapes)
{
    shape.Describe();
}

ISpeaker[] speakers = { new Robot(), new Doorbell() };
foreach (ISpeaker speaker in speakers)
{
    speaker.Speak();
}

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

abstract class Shape
{
    public string Name { get; set; } = "";
    public double Width { get; set; }
    public double Height { get; set; }

    public abstract double Area();

    public void Describe()
    {
        Console.WriteLine($"{Name} area: {Area()}");
    }
}

class Rectangle : Shape
{
    public override double Area()
    {
        return Width * Height;
    }
}

class Triangle : Shape
{
    public override double Area()
    {
        return Width * Height / 2;
    }
}

interface ISpeaker
{
    void Speak();
}

class Robot : ISpeaker
{
    public void Speak()
    {
        Console.WriteLine("Robot says: Beep!");
    }
}

class Doorbell : ISpeaker
{
    public void Speak()
    {
        Console.WriteLine("Doorbell says: Ding dong!");
    }
}
