namespace OrderSystem.Web.Models;

public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.NEW;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
