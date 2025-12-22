using MySqlConnector;
using OrderSystem.Web.Data;
using OrderSystem.Web.Dto;

namespace OrderSystem.Web.Services;

/// <summary>
/// Úprava stavu "zaplaceno/nezaplaceno" pro existující objednávku.
/// Dělá změny do více tabulek (orders + payments) v transakci.
/// </summary>
public sealed class OrderPaymentService
{
    private readonly MySqlConnectionFactory _db;

    public OrderPaymentService(MySqlConnectionFactory db) => _db = db;

    public async Task SetPaidAsync(int orderId, SetOrderPaidRequest req, CancellationToken ct)
    {
        if (orderId <= 0) throw new ArgumentException("Order id must be > 0.");
        if (req.Amount is not null && req.Amount < 0) throw new ArgumentException("Amount must be >= 0.");

        await using var conn = _db.Create();
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            const string orderSql = @"SELECT state FROM orders WHERE id=@id FOR UPDATE;";
            await using var orderCmd = new MySqlCommand(orderSql, conn, (MySqlTransaction)tx);
            orderCmd.Parameters.AddWithValue("@id", orderId);
            var statusObj = await orderCmd.ExecuteScalarAsync(ct);
            if (statusObj is null) throw new InvalidOperationException($"Order id={orderId} not found.");

            var status = statusObj.ToString() ?? "new";
            if (status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot change payment of a cancelled order.");
            
            const string payFindSql = @"SELECT id FROM payments WHERE order_id=@oid ORDER BY id DESC LIMIT 1 FOR UPDATE;";
            await using var payFind = new MySqlCommand(payFindSql, conn, (MySqlTransaction)tx);
            payFind.Parameters.AddWithValue("@oid", orderId);
            var payIdObj = await payFind.ExecuteScalarAsync(ct);
            int? paymentId = payIdObj is null ? null : Convert.ToInt32(payIdObj);

            if (req.Paid)
            {
                var amount = req.Amount ?? await GetOrderTotalAsync(conn, (MySqlTransaction)tx, orderId, ct);

                if (paymentId is null)
                {
                    const string insertPaySql = @"INSERT INTO payments(order_id, amount, paid, paid_at, provider) VALUES(@oid,@amount,1,CURRENT_TIMESTAMP,@provider);";
                    await using var ins = new MySqlCommand(insertPaySql, conn, (MySqlTransaction)tx);
                    ins.Parameters.AddWithValue("@oid", orderId);
                    ins.Parameters.AddWithValue("@amount", amount);
                    ins.Parameters.AddWithValue("@provider", (object?)req.Provider ?? DBNull.Value);
                    await ins.ExecuteNonQueryAsync(ct);
                }
                else
                {
                    const string updatePaySql = @"UPDATE payments SET amount=@amount, paid=1, paid_at=CURRENT_TIMESTAMP, provider=@provider WHERE id=@pid;";
                    await using var upd = new MySqlCommand(updatePaySql, conn, (MySqlTransaction)tx);
                    upd.Parameters.AddWithValue("@pid", paymentId.Value);
                    upd.Parameters.AddWithValue("@amount", amount);
                    upd.Parameters.AddWithValue("@provider", (object?)req.Provider ?? DBNull.Value);
                    await upd.ExecuteNonQueryAsync(ct);
                }
                
                const string markPaidSql = @"UPDATE orders SET state='paid' WHERE id=@oid;";
                await using var mark = new MySqlCommand(markPaidSql, conn, (MySqlTransaction)tx);
                mark.Parameters.AddWithValue("@oid", orderId);
                await mark.ExecuteNonQueryAsync(ct);
            }
            else
            {
                if (paymentId is not null)
                {
                    const string unpaidSql = @"UPDATE payments SET paid=0, paid_at=NULL WHERE id=@pid;";
                    await using var un = new MySqlCommand(unpaidSql, conn, (MySqlTransaction)tx);
                    un.Parameters.AddWithValue("@pid", paymentId.Value);
                    await un.ExecuteNonQueryAsync(ct);
                }
                
                const string markNewSql = @"UPDATE orders SET state='new' WHERE id=@oid;";
                await using var mark = new MySqlCommand(markNewSql, conn, (MySqlTransaction)tx);
                mark.Parameters.AddWithValue("@oid", orderId);
                await mark.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task<decimal> GetOrderTotalAsync(MySqlConnection conn, MySqlTransaction tx, int orderId, CancellationToken ct)
    {
        const string sql = @"SELECT COALESCE(SUM(quantity * unit_price), 0) AS total FROM order_items WHERE order_id=@oid;";
        await using var cmd = new MySqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@oid", orderId);
        var obj = await cmd.ExecuteScalarAsync(ct);
        return obj is null ? 0 : Convert.ToDecimal(obj);
    }
}
