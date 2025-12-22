using MySqlConnector;

namespace OrderSystem.Web.Data.Repositories;

public static class MySqlDataReaderExtensions
{
    public static int GetInt32(this MySqlDataReader r, string name) => r.GetInt32(r.GetOrdinal(name));
    public static string GetString(this MySqlDataReader r, string name) => r.GetString(r.GetOrdinal(name));
    public static decimal GetDecimal(this MySqlDataReader r, string name) => r.GetDecimal(r.GetOrdinal(name));
    public static bool GetBoolean(this MySqlDataReader r, string name) => r.GetBoolean(r.GetOrdinal(name));
    public static float GetFloat(this MySqlDataReader r, string name) => r.GetFloat(r.GetOrdinal(name));
    public static DateTime GetDateTime(this MySqlDataReader r, string name) => r.GetDateTime(r.GetOrdinal(name));
    public static bool IsDBNull(this MySqlDataReader r, string name) => r.IsDBNull(r.GetOrdinal(name));
}
