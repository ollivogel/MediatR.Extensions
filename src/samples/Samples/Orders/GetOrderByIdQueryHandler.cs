namespace Samples.Orders;

using MediatR;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderResponse>
{
  public Task<OrderResponse> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
  {
    // Simulates a database lookup
    return Task.FromResult(new OrderResponse
    {
      OrderId = request.OrderId,
      ProductName = "Widget",
      Amount = 29.99m,
      Status = "Shipped",
    });
  }
}
