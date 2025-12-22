using MySqlConnector;

namespace OrderSystem.Web.Data.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly MySqlConnectionFactory _db;

    public ReportRepository(MySqlConnectionFactory db) => _db = db;

    public async Task<List<TopCustomerRow>> GetTopCustomersAsync(int limit, CancellationToken ct)
    {
        const string sql = @"
            SELECT
              c.id AS customer_id,
              CONCAT(c.first_name,' ',c.last_name) AS customer_name,
              COUNT(DISTINCT o.id) AS orders_count,
              COALESCE(SUM(oi.quantity * oi.unit_price),0) AS total_spent
            FROM customers c
            LEFT JOIN orders o ON o.customer_id = c.id
            LEFT JOIN order_items oi ON oi.order_id = o.id
            GROUP BY c.id, customer_name
            ORDER BY total_spent DESC
            LIMIT @lim;";

        await using var conn = _db.Create();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@lim", limit);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<TopCustomerRow>();
        while (await r.ReadAsync(ct))
        {
            list.Add(new TopCustomerRow(
                r.GetInt32("customer_id"),
                r.GetString("customer_name"),
                r.GetInt32("orders_count"),
                r.GetDecimal("total_spent")
            ));
        }
        return list;
    }

    public async Task<List<ProductSalesRow>> GetProductSalesAsync(CancellationToken ct)
    {
        const string sql = @"SELECT product_id, name, qty_sold, revenue FROM v_product_sales ORDER BY revenue DESC;";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand(sql, conn);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<ProductSalesRow>();
        while (await r.ReadAsync(ct))
        {
            list.Add(new ProductSalesRow(
                r.GetInt32("product_id"),
                r.GetString("name"),
                r.GetInt32("qty_sold"),
                r.GetDecimal("revenue")
            ));
        }
        return list;
    }

    public async Task<List<OrderTotalRow>> GetOrderTotalsAsync(CancellationToken ct)
    {
        const string sql = @"SELECT order_id, customer_id, customer_name, status, created_at, total_amount FROM v_order_totals ORDER BY order_id DESC;";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand(sql, conn);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<OrderTotalRow>();
        while (await r.ReadAsync(ct))
        {
            list.Add(new OrderTotalRow(
                r.GetInt32("order_id"),
                r.GetInt32("customer_id"),
                r.GetString("customer_name"),
                r.GetString("status"),
                r.GetDateTime("created_at"),
                r.GetDecimal("total_amount")
            ));
        }
        return list;
    }
}
