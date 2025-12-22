using OrderSystem.Web.Models;

namespace OrderSystem.Web.Dto;

public sealed class CreateOrderRequest
{
    public int CustomerId { get; set; }
    public string? Note { get; set; }
    public List<CreateOrderItem> Items { get; set; } = new();
    public CreatePayment? Payment { get; set; }
}

public sealed class CreateOrderItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public sealed class CreatePayment
{
    public decimal Amount { get; set; }
    public bool Paid { get; set; }
    public string? Provider { get; set; }
}
