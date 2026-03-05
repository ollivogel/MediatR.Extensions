namespace Samples.Orders;

using MediatR;
using MediatR.Extensions.Common;

[MediatorGroup("Orders")]
public class GetOrderByIdQuery(Guid orderId) : IRequest<OrderResponse>
{
  public Guid OrderId { get; } = orderId;
}
