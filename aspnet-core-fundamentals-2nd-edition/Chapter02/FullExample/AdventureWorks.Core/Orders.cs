// Orders.cs
namespace AdventureWorks.Core;
public sealed record Order(int Id, decimal Total);
public interface IOrderRepository { Order? GetById(int id); }
public interface ICacheService
{
    string? Get(string key);
    void Set(string key, string value);
}
public interface IEmailValidator { bool IsValid(string address); }
