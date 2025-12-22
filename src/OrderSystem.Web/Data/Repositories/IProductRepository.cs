using OrderSystem.Web.Models;

namespace OrderSystem.Web.Data.Repositories;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync(CancellationToken ct);
    Task<Product?> GetByIdAsync(int id, CancellationToken ct);
    Task<int> CreateAsync(Product product, CancellationToken ct);
    Task<bool> UpdateAsync(Product product, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
