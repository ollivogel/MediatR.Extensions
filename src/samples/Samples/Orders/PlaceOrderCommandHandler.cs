namespace Samples.Orders;

using MediatR;

public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, OrderResponse>
{
  public Task<OrderResponse> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
  {
    return Task.FromResult(new OrderResponse
    {
      OrderId = Guid.NewGuid(),
      ProductName = request.ProductName,
      Amount = request.Amount,
      Status = "Confirmed",
    });
  }
}
