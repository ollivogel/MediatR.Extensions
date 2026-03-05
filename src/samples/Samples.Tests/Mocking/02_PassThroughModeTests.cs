namespace Samples.Tests.Mocking;

using FluentAssertions;
using MediatR;
using MediatR.Extensions.Mocking;
using MediatR.Extensions.Mocking.Registration.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Samples.Orders;
using Xunit;

/// <summary>
/// PassThrough Mode lets unmocked requests fall through to the real handler.
/// Only requests with an explicit mock registration are intercepted.
///
/// Use this for integration-style tests where you only want to mock specific handlers.
/// </summary>
public class PassThroughModeTests
{
  private static ServiceProvider CreateServices(Action<MediatorMockingConfiguration> configure)
  {
    return new ServiceCollection()
      .AddMediatorMocking(configure)
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>())
      .BuildServiceProvider();
  }

  [Fact]
  public async Task PassThrough_Calls_Real_Handler_When_No_Mock_Registered()
  {
    // Arrange - PassThrough mode, no mock for PlaceOrderCommand
    var provider = CreateServices(c => { c.RuntimeMode = RuntimeMode.PassThrough; });

    var mediator = provider.GetRequiredService<IMediator>();

    // Act - The real PlaceOrderCommandHandler executes
    var result = await mediator.Send(new PlaceOrderCommand
    {
      ProductName = "Real Widget",
      Amount = 49.99m,
    });

    // Assert - Real handler returns actual data
    result.ProductName.Should().Be("Real Widget");
    result.Amount.Should().Be(49.99m);
    result.Status.Should().Be("Confirmed"); // Set by the real handler
  }

  [Fact]
  public async Task PassThrough_Uses_Mock_When_Registered()
  {
    // Arrange - Mock only PlaceOrder, let others pass through
    var mocked = new OrderResponse { Status = "Mocked" };

    var provider = CreateServices(c =>
    {
      c.RuntimeMode = RuntimeMode.PassThrough;
      c.MockRegistration.Orders.PlaceOrder.WithReturnValue(mocked);
    });

    var mediator = provider.GetRequiredService<IMediator>();

    // Act - PlaceOrder is mocked, GetOrderById passes through
    var orderId = Guid.NewGuid();
    var placedOrder = await mediator.Send(new PlaceOrderCommand { ProductName = "Widget" });
    var fetchedOrder = await mediator.Send(new GetOrderByIdQuery(orderId));

    // Assert
    placedOrder.Status.Should().Be("Mocked"); // Mock intercepted
    fetchedOrder.Status.Should().Be("Shipped"); // Real handler executed
    fetchedOrder.OrderId.Should().Be(orderId);
  }

  [Fact]
  public async Task PassThrough_Works_For_Void_Commands()
  {
    // Arrange
    var provider = CreateServices(c => { c.RuntimeMode = RuntimeMode.PassThrough; });

    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert - No exception, real handler runs
    await mediator
      .Invoking(m => m.Send(new CancelOrderCommand { OrderId = Guid.NewGuid(), Reason = "Test" }))
      .Should()
      .NotThrowAsync();
  }
}
