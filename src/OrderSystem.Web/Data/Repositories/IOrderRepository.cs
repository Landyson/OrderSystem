using OrderSystem.Web.Models;

namespace OrderSystem.Web.Data.Repositories;

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync(CancellationToken ct);
    Task<Order?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<OrderItem>> GetItemsAsync(int orderId, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
