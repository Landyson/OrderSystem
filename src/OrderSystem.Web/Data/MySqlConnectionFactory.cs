using Microsoft.Extensions.Options;
using MySqlConnector;

namespace OrderSystem.Web.Data;

public sealed class MySqlConnectionFactory
{
    private readonly string _connStr;

    public MySqlConnectionFactory(IConfiguration config)
    {
        _connStr = config.GetConnectionString("MySql")
                  ?? throw new InvalidOperationException("Missing ConnectionStrings:MySql in appsettings.json");
    }

    public MySqlConnection Create()
    {
        return new MySqlConnection(_connStr);
    }
}
