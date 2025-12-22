using MySqlConnector;
using OrderSystem.Web.Data;
using OrderSystem.Web.Dto;

namespace OrderSystem.Web.Services;

public sealed class OrderService
{
    private readonly MySqlConnectionFactory _db;

    public OrderService(MySqlConnectionFactory db) => _db = db;

    /// <summary>
    /// Transakční vytvoření objednávky:
    /// - orders INSERT
    /// - order_items INSERT (n řádků)
    /// - products UPDATE (odečet skladu)
    /// - payments INSERT (volitelně)
    /// </summary>
    public async Task<int> CreateOrderAsync(CreateOrderRequest req, CancellationToken ct)
    {
        Validate(req);

        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            const string insertOrderSql = @"INSERT INTO orders(customer_id, state, note) VALUES(@cid, 'new', @note); SELECT LAST_INSERT_ID();";
            await using var insertOrder = new MySqlCommand(insertOrderSql, conn, (MySqlTransaction)tx);
            insertOrder.Parameters.AddWithValue("@cid", req.CustomerId);
            insertOrder.Parameters.AddWithValue("@note", (object?)req.Note ?? DBNull.Value);
            var orderId = Convert.ToInt32(await insertOrder.ExecuteScalarAsync(ct));
            
            foreach (var item in req.Items)
            {
                const string selectProductSql = @"SELECT price, stock, is_active FROM products WHERE id=@pid FOR UPDATE;";
                await using var selectProd = new MySqlCommand(selectProductSql, conn, (MySqlTransaction)tx);
                selectProd.Parameters.AddWithValue("@pid", item.ProductId);

                await using var r = await selectProd.ExecuteReaderAsync(ct);
                if (!await r.ReadAsync(ct))
                    throw new InvalidOperationException($"Product id={item.ProductId} does not exist.");

                var price = r.GetDecimal("price");
                var stock = r.GetInt32("stock");
                var isActive = r.GetBoolean("is_active");
                await r.CloseAsync();

                if (!isActive)
                    throw new InvalidOperationException($"Product id={item.ProductId} is inactive.");

                if (stock < item.Quantity)
                    throw new InvalidOperationException($"Not enough stock for product id={item.ProductId}. Stock={stock}, requested={item.Quantity}");

                const string insertItemSql = @"INSERT INTO order_items(order_id, product_id, quantity, unit_price) VALUES(@oid,@pid,@qty,@price);";
                await using var insertItem = new MySqlCommand(insertItemSql, conn, (MySqlTransaction)tx);
                insertItem.Parameters.AddWithValue("@oid", orderId);
                insertItem.Parameters.AddWithValue("@pid", item.ProductId);
                insertItem.Parameters.AddWithValue("@qty", item.Quantity);
                insertItem.Parameters.AddWithValue("@price", price);
                await insertItem.ExecuteNonQueryAsync(ct);

                const string updateStockSql = @"UPDATE products SET stock = stock - @qty WHERE id=@pid;";
                await using var updateStock = new MySqlCommand(updateStockSql, conn, (MySqlTransaction)tx);
                updateStock.Parameters.AddWithValue("@qty", item.Quantity);
                updateStock.Parameters.AddWithValue("@pid", item.ProductId);
                await updateStock.ExecuteNonQueryAsync(ct);
            }
            
            if (req.Payment is not null)
            {
                const string insertPaySql = @"INSERT INTO payments(order_id, amount, paid, paid_at, provider) VALUES(@oid,@amount,@paid, CASE WHEN @paid=1 THEN CURRENT_TIMESTAMP ELSE NULL END, @provider);";
                await using var insertPay = new MySqlCommand(insertPaySql, conn, (MySqlTransaction)tx);
                insertPay.Parameters.AddWithValue("@oid", orderId);
                insertPay.Parameters.AddWithValue("@amount", req.Payment.Amount);
                insertPay.Parameters.AddWithValue("@paid", req.Payment.Paid);
                insertPay.Parameters.AddWithValue("@provider", (object?)req.Payment.Provider ?? DBNull.Value);
                await insertPay.ExecuteNonQueryAsync(ct);

                if (req.Payment.Paid)
                {
                    const string markPaidSql = @"UPDATE orders SET state='paid' WHERE id=@oid;";
                    await using var markPaid = new MySqlCommand(markPaidSql, conn, (MySqlTransaction)tx);
                    markPaid.Parameters.AddWithValue("@oid", orderId);
                    await markPaid.ExecuteNonQueryAsync(ct);
                }
            }

            await tx.CommitAsync(ct);
            return orderId;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static void Validate(CreateOrderRequest req)
    {
        if (req.CustomerId <= 0) throw new ArgumentException("CustomerId must be > 0.");
        if (req.Items is null || req.Items.Count == 0) throw new ArgumentException("Order must contain at least one item.");
        foreach (var i in req.Items)
        {
            if (i.ProductId <= 0) throw new ArgumentException("Item.ProductId must be > 0.");
            if (i.Quantity <= 0) throw new ArgumentException("Item.Quantity must be > 0.");
        }
        if (req.Payment is not null && req.Payment.Amount < 0) throw new ArgumentException("Payment.Amount must be >= 0.");
    }
}
