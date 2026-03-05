namespace Samples.Orders;

public class OrderResponse
{
  public Guid OrderId { get; set; }
  public string ProductName { get; set; } = string.Empty;
  public decimal Amount { get; set; }
  public string Status { get; set; } = "Pending";
}
