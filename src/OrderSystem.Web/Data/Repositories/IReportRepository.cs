namespace OrderSystem.Web.Data.Repositories;

public interface IReportRepository
{
    Task<List<TopCustomerRow>> GetTopCustomersAsync(int limit, CancellationToken ct);
    Task<List<ProductSalesRow>> GetProductSalesAsync(CancellationToken ct);
    Task<List<OrderTotalRow>> GetOrderTotalsAsync(CancellationToken ct);
}

public sealed record TopCustomerRow(int CustomerId, string CustomerName, int OrdersCount, decimal TotalSpent);
public sealed record ProductSalesRow(int ProductId, string Name, int QtySold, decimal Revenue);
public sealed record OrderTotalRow(int OrderId, int CustomerId, string CustomerName, string Status, DateTime CreatedAt, decimal TotalAmount);
