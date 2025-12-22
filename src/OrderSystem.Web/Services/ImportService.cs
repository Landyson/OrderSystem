using System.Text.Json;
using MySqlConnector;
using OrderSystem.Web.Data;
using OrderSystem.Web.Models;

namespace OrderSystem.Web.Services;

public sealed class ImportService
{
    private readonly MySqlConnectionFactory _db;

    public ImportService(MySqlConnectionFactory db) => _db = db;

    public async Task<int> ImportCustomersCsvAsync(Stream csvStream, CancellationToken ct)
    {
        using var reader = new StreamReader(csvStream, leaveOpen: true);
        var header = (await reader.ReadLineAsync()) ?? "";
        if (!header.Trim().Equals("first_name,last_name,email,phone", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("CSV header must be: first_name,last_name,email,phone");

        int inserted = 0;
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            if (parts.Length < 4) throw new ArgumentException($"Invalid CSV line: {line}");

            var c = new Customer
            {
                FirstName = parts[0].Trim(),
                LastName = parts[1].Trim(),
                Email = parts[2].Trim(),
                Phone = string.IsNullOrWhiteSpace(parts[3]) ? null : parts[3].Trim()
            };

            ValidateCustomer(c);

            const string sql = @"INSERT INTO customers(first_name,last_name,email,phone) VALUES(@fn,@ln,@em,@ph) ON DUPLICATE KEY UPDATE first_name=VALUES(first_name), last_name=VALUES(last_name), phone=VALUES(phone);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@fn", c.FirstName);
            cmd.Parameters.AddWithValue("@ln", c.LastName);
            cmd.Parameters.AddWithValue("@em", c.Email);
            cmd.Parameters.AddWithValue("@ph", (object?)c.Phone ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync(ct);
            if (rows > 0) inserted++;
        }

        return inserted;
    }

    public async Task<int> ImportProductsJsonAsync(Stream jsonStream, CancellationToken ct)
    {
        var products = await JsonSerializer.DeserializeAsync<List<Product>>(jsonStream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }, ct) ?? throw new ArgumentException("Invalid JSON.");

        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        int inserted = 0;
        foreach (var p in products)
        {
            ValidateProduct(p);

            const string sql = @"INSERT INTO products(name,price,stock,is_active,rating) VALUES(@name,@price,@stock,@active,@rating) ON DUPLICATE KEY UPDATE name=VALUES(name), price=VALUES(price), stock=VALUES(stock), is_active=VALUES(is_active), rating=VALUES(rating);";
            await using var cmd = new MySqlCommand(sql, conn);            
            cmd.Parameters.AddWithValue("@name", p.Name);
            cmd.Parameters.AddWithValue("@price", p.Price);
            cmd.Parameters.AddWithValue("@stock", p.Stock);
            cmd.Parameters.AddWithValue("@active", p.IsActive);
            cmd.Parameters.AddWithValue("@rating", (object?)p.Rating ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync(ct);
            if (rows > 0) inserted++;
        }

        return inserted;
    }

    private static void ValidateCustomer(Customer c)
    {
        if (string.IsNullOrWhiteSpace(c.FirstName)) throw new ArgumentException("first_name is required.");
        if (string.IsNullOrWhiteSpace(c.LastName)) throw new ArgumentException("last_name is required.");
        if (string.IsNullOrWhiteSpace(c.Email) || !c.Email.Contains('@')) throw new ArgumentException("email is required and must contain @.");
    }

    private static void ValidateProduct(Product p)
    {
                if (string.IsNullOrWhiteSpace(p.Name)) throw new ArgumentException("name is required.");
        if (p.Price < 0) throw new ArgumentException("price must be >= 0.");
        if (p.Stock < 0) throw new ArgumentException("stock must be >= 0.");
    }
}
