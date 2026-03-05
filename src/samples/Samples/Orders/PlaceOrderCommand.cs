namespace Samples.Orders;

using MediatR;
using MediatR.Extensions.Common;

[MediatorGroup("Orders")]
public class PlaceOrderCommand : IRequest<OrderResponse>
{
  public string ProductName { get; set; } = string.Empty;
  public decimal Amount { get; set; }
}
