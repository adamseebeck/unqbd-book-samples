namespace AdventureWorks.Core;

public sealed record Order(int Id, string OrderNumber,
    string OwnerId, decimal Total, bool Archived = false);

public interface IOrderRepository
{
    Order? GetById(int id);
    Order Add(string ownerId, decimal total);
    bool Delete(int id);
    bool Archive(int id);
}

public interface ICacheService
{
    string? Get(string key);
    void Set(string key, string value);
}

public interface IEmailValidator
{
    bool IsValid(string address);
}
