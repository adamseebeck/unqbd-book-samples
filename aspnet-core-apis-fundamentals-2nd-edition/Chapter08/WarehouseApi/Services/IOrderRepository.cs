using WarehouseApi.Contracts;
using WarehouseApi.Models;

namespace WarehouseApi.Services;

public interface IOrderRepository
{
    void Add(Order order);
    Order? GetById(Guid id);
    IReadOnlyList<Order> GetAll();
    bool TryUpdateStatus(Guid id, OrderStatus currentStatus, OrderStatus newStatus);
    bool Remove(Guid id);
}
