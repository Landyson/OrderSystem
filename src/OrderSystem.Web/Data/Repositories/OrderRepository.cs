using MySqlConnector;
using OrderSystem.Web.Models;

namespace OrderSystem.Web.Data.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly MySqlConnectionFactory _db;

    public OrderRepository(MySqlConnectionFactory db) => _db = db;

    public async Task<List<Order>> GetAllAsync(CancellationToken ct)
    {
        const string sql = @"SELECT id, customer_id, state AS status, note, created_at FROM orders ORDER BY id DESC;";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<Order>();
        while (await r.ReadAsync(ct))
        {
            list.Add(new Order
            {
                Id = r.GetInt32("id"),
                CustomerId = r.GetInt32("customer_id"),
                Status = Enum.Parse<OrderStatus>(r.GetString("status"), ignoreCase: true),
                Note = r.IsDBNull("note") ? null : r.GetString("note"),
                CreatedAt = r.GetDateTime("created_at")
            });
        }
        return list;
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken ct)
    {
        const string sql = @"SELECT id, customer_id, state AS status, note, created_at FROM orders WHERE id=@id;";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new Order
        {
            Id = r.GetInt32("id"),
            CustomerId = r.GetInt32("customer_id"),
            Status = Enum.Parse<OrderStatus>(r.GetString("status"), ignoreCase: true),
            Note = r.IsDBNull("note") ? null : r.GetString("note"),
            CreatedAt = r.GetDateTime("created_at")
        };
    }

    public async Task<List<OrderItem>> GetItemsAsync(int orderId, CancellationToken ct)
    {
        const string sql = @"SELECT order_id, product_id, quantity, unit_price FROM order_items WHERE order_id=@id;";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", orderId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<OrderItem>();
        while (await r.ReadAsync(ct))
        {
            list.Add(new OrderItem
            {
                OrderId = r.GetInt32("order_id"),
                ProductId = r.GetInt32("product_id"),
                Quantity = r.GetInt32("quantity"),
                UnitPrice = r.GetDecimal("unit_price")
            });
        }
        return list;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        const string sql = @"DELETE FROM orders WHERE id=@id;";
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@id", id);

        await cmd.ExecuteNonQueryAsync(ct);
    }

}
