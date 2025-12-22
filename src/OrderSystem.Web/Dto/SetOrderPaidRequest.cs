namespace OrderSystem.Web.Dto;

public sealed class SetOrderPaidRequest
{
    public bool Paid { get; set; }
    public decimal? Amount { get; set; }
    public string? Provider { get; set; }
}
