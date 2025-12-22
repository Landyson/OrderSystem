using MySqlConnector;
using OrderSystem.Web.Models;

namespace OrderSystem.Web.Data.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly MySqlConnectionFactory _db;

    public CustomerRepository(MySqlConnectionFactory db) => _db = db;

    public async Task<List<Customer>> GetAllAsync(CancellationToken ct)
    {
        const string sql = @"SELECT id, first_name, last_name, email, phone, created_at FROM customers ORDER BY id DESC;";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<Customer>();
        while (await r.ReadAsync(ct))
        {
            list.Add(new Customer
            {
                Id = r.GetInt32("id"),
                FirstName = r.GetString("first_name"),
                LastName = r.GetString("last_name"),
                Email = r.GetString("email"),
                Phone = r.IsDBNull("phone") ? null : r.GetString("phone"),
                CreatedAt = r.GetDateTime("created_at")
            });
        }
        return list;
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct)
    {
        const string sql = @"SELECT id, first_name, last_name, email, phone, created_at FROM customers WHERE id=@id;";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new Customer
        {
            Id = r.GetInt32("id"),
            FirstName = r.GetString("first_name"),
            LastName = r.GetString("last_name"),
            Email = r.GetString("email"),
            Phone = r.IsDBNull("phone") ? null : r.GetString("phone"),
            CreatedAt = r.GetDateTime("created_at")
        };
    }

    public async Task<int> CreateAsync(Customer c, CancellationToken ct)
    {
        const string sql = @"INSERT INTO customers(first_name,last_name,email,phone) VALUES(@fn,@ln,@em,@ph); SELECT LAST_INSERT_ID();";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@fn", c.FirstName);
        cmd.Parameters.AddWithValue("@ln", c.LastName);
        cmd.Parameters.AddWithValue("@em", c.Email);
        cmd.Parameters.AddWithValue("@ph", (object?)c.Phone ?? DBNull.Value);

        var idObj = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(idObj);
    }

    public async Task<bool> UpdateAsync(Customer c, CancellationToken ct)
    {
        const string sql = @"UPDATE customers SET first_name=@fn,last_name=@ln,email=@em,phone=@ph WHERE id=@id;";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", c.Id);
        cmd.Parameters.AddWithValue("@fn", c.FirstName);
        cmd.Parameters.AddWithValue("@ln", c.LastName);
        cmd.Parameters.AddWithValue("@em", c.Email);
        cmd.Parameters.AddWithValue("@ph", (object?)c.Phone ?? DBNull.Value);

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        return rows == 1;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        const string sql = @"DELETE FROM customers WHERE id=@id;";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        return rows == 1;
    }
}
