namespace Samples.Orders;

using MediatR;
using MediatR.Extensions.Common;

[MediatorGroup("Orders")]
public class CancelOrderCommand : IRequest
{
  public Guid OrderId { get; set; }
  public string Reason { get; set; } = string.Empty;
}
