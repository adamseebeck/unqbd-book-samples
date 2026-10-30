using WarehouseApi.Contracts;
using WarehouseApi.Models;

namespace WarehouseApi.Services;

// Registered as a Singleton, so every request reads and writes this one store
public class InMemoryOrderRepository : IOrderRepository
{
    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, Order> _orders = new();

    public void Add(Order order)
    {
        lock (_lock)
        {
            _orders.Add(order.Id, order);
        }
    }

    public Order? GetById(Guid id)
    {
        lock (_lock)
        {
            return _orders.GetValueOrDefault(id);
        }
    }

    public IReadOnlyList<Order> GetAll()
    {
        lock (_lock)
        {
            // A copy, so callers can enumerate it while other requests add orders;
            // the ID breaks ties, so orders created at the same moment keep their page
            return _orders.Values.OrderBy(o => o.CreatedDate).ThenBy(o => o.Id).ToList();
        }
    }

    public bool TryUpdateStatus(Guid id, OrderStatus currentStatus, OrderStatus newStatus)
    {
        lock (_lock)
        {
            // Refuse the change if another request moved the order after the caller read it
            if (!_orders.TryGetValue(id, out var order) || order.Status != currentStatus)
            {
                return false;
            }

            order.Status = newStatus;
            return true;
        }
    }

    public bool Remove(Guid id)
    {
        lock (_lock)
        {
            return _orders.Remove(id);
        }
    }
}
