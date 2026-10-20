using MyProject.Models;
using MyProject.Services;

Person person = new Person { Name = "Maria" };
GreetingService service = new GreetingService();
service.Greet(person);

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
}
