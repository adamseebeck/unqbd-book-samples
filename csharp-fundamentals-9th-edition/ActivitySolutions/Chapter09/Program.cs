Animal[] animals =
{
    new Dog { Name = "Buddy" },
    new Cat { Name = "Luna" },
    new Bird { Name = "Tweety" }
};
foreach (Animal animal in animals) animal.Speak();

class Animal
{
    public string Name { get; set; } = "";
    public virtual void Speak() { Console.WriteLine($"{Name} makes a sound."); }
}
class Dog : Animal
{
    public override void Speak() { Console.WriteLine("Woof!"); }
}
class Cat : Animal
{
    public override void Speak() { Console.WriteLine("Meow!"); }
}
class Bird : Animal
{
    public override void Speak() { Console.WriteLine("Tweet!"); }
}
