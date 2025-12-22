using MySqlConnector;
using OrderSystem.Web.Models;

namespace OrderSystem.Web.Data.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly MySqlConnectionFactory _db;

    public ProductRepository(MySqlConnectionFactory db) => _db = db;

    public async Task<List<Product>> GetAllAsync(CancellationToken ct)
    {
        const string sql = @"SELECT id, name, price, stock, is_active, rating, created_at FROM products ORDER BY id DESC;";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<Product>();
        while (await r.ReadAsync(ct))
        {
            list.Add(new Product
            {
                Id = r.GetInt32("id"),                
                Name = r.GetString("name"),
                Price = r.GetDecimal("price"),
                Stock = r.GetInt32("stock"),
                IsActive = r.GetBoolean("is_active"),
                Rating = r.IsDBNull("rating") ? null : r.GetFloat("rating"),
                CreatedAt = r.GetDateTime("created_at")
            });
        }
        return list;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct)
    {
        const string sql = @"SELECT id, name, price, stock, is_active, rating, created_at FROM products WHERE id=@id;";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new Product
        {
            Id = r.GetInt32("id"),            
            Name = r.GetString("name"),
            Price = r.GetDecimal("price"),
            Stock = r.GetInt32("stock"),
            IsActive = r.GetBoolean("is_active"),
            Rating = r.IsDBNull("rating") ? null : r.GetFloat("rating"),
            CreatedAt = r.GetDateTime("created_at")
        };
    }

    public async Task<int> CreateAsync(Product p, CancellationToken ct)
    {
        const string sql = @"INSERT INTO products(name,price,stock,is_active,rating) VALUES(@name,@price,@stock,@active,@rating); SELECT LAST_INSERT_ID();";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);        
        cmd.Parameters.AddWithValue("@name", p.Name);
        cmd.Parameters.AddWithValue("@price", p.Price);
        cmd.Parameters.AddWithValue("@stock", p.Stock);
        cmd.Parameters.AddWithValue("@active", p.IsActive);
        cmd.Parameters.AddWithValue("@rating", (object?)p.Rating ?? DBNull.Value);

        var idObj = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(idObj);
    }

    public async Task<bool> UpdateAsync(Product p, CancellationToken ct)
    {
        const string sql = @"UPDATE products SET name=@name,price=@price,stock=@stock,is_active=@active,rating=@rating WHERE id=@id;";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", p.Id);        
        cmd.Parameters.AddWithValue("@name", p.Name);
        cmd.Parameters.AddWithValue("@price", p.Price);
        cmd.Parameters.AddWithValue("@stock", p.Stock);
        cmd.Parameters.AddWithValue("@active", p.IsActive);
        cmd.Parameters.AddWithValue("@rating", (object?)p.Rating ?? DBNull.Value);

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        return rows == 1;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        const string sql = @"DELETE FROM products WHERE id=@id;";
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        return rows == 1;
    }
}
