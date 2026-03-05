namespace Samples.Orders;

using MediatR;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand>
{
  public Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
  {
    // Simulates cancellation logic
    Console.WriteLine($"Order {request.OrderId} cancelled: {request.Reason}");
    return Task.CompletedTask;
  }
}
