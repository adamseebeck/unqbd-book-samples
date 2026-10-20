using OrganizedGreeting.Models;
using OrganizedGreeting.Services;

Person person = new Person { FirstName = "Maria", LastName = "Smith" };
Greeter greeter = new Greeter();
greeter.Greet(person);
FarewellService farewell = new FarewellService();
farewell.SayGoodbye(person);

namespace OrganizedGreeting.Models
{
    public class Person
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }
}

namespace OrganizedGreeting.Services
{
    public class Greeter
    {
        public void Greet(Person person)
        {
            Console.WriteLine($"Hello, {person.FirstName} {person.LastName}!");
        }
    }
    public class FarewellService
    {
        public void SayGoodbye(Person person)
        {
            Console.WriteLine($"Goodbye, {person.FirstName} {person.LastName}!");
        }
    }
}
