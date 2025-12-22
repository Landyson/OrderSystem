namespace OrderSystem.Web.Models;

public sealed class Product
{
    public int Id { get; set; }    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public float? Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}
