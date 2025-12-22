namespace OrderSystem.Web.Models;

public sealed class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public bool Paid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Provider { get; set; }
}
