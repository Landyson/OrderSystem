using OrderSystem.Web.Models;

namespace OrderSystem.Web.Data.Repositories;

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync(CancellationToken ct);
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct);
    Task<int> CreateAsync(Customer customer, CancellationToken ct);
    Task<bool> UpdateAsync(Customer customer, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
