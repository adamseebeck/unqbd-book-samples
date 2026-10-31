using MyProject.Models;
using MyProject.Services;

Person person = new Person { Name = "Maria" };
GreetingService service = new GreetingService();
service.Greet(person);
Console.WriteLine(Calculator.Add(2, 3));
Console.WriteLine(Calculator.Subtract(10, 4));

namespace MyProject.Models
{
    public class Person
    {
        public string Name { get; set; } = "";
    }
}

namespace MyProject.Services
{
    public class GreetingService
    {
        public void Greet(Person person)
        {
            Console.WriteLine($"Hello, {person.Name}!");
        }
    }

    public static class Calculator
    {
        public static int Add(int a, int b)
        {
            return a + b;
        }

        public static int Subtract(int a, int b)
        {
            return a - b;
        }
    }
}
