namespace Samples.Tests.Mocking;

using FluentAssertions;
using MediatR;
using MediatR.Extensions.Mocking;
using MediatR.Extensions.Mocking.Registration.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Samples.Orders;
using Xunit;

/// <summary>
/// Fallback Mode creates default-constructed instances for unmocked requests.
/// The real handler is never called - instead, a new TResponse() is created via Activator.
///
/// Use this when you don't care about specific return values for unmocked requests
/// and just want the pipeline to not throw.
/// </summary>
public class FallbackModeTests
{
  private static ServiceProvider CreateServices(Action<MediatorMockingConfiguration> configure)
  {
    return new ServiceCollection()
      .AddMediatorMocking(configure)
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>())
      .BuildServiceProvider();
  }

  [Fact]
  public async Task Fallback_Returns_Default_Instance_For_Unmocked_Requests()
  {
    // Arrange
    var provider = CreateServices(c => { c.RuntimeMode = RuntimeMode.Fallback; });

    var mediator = provider.GetRequiredService<IMediator>();

    // Act - No mock registered, Fallback creates a default instance via Activator.CreateInstance
    var result = await mediator.Send(new PlaceOrderCommand { ProductName = "Widget" });

    // Assert - A new default-constructed OrderResponse is returned (not null)
    result.Should().NotBeNull();
    result.ProductName.Should().BeEmpty();
    result.Amount.Should().Be(0);
    result.Status.Should().Be("Pending"); // Default value from OrderResponse constructor
  }

  [Fact]
  public async Task Fallback_Does_Not_Throw_For_Void_Commands()
  {
    // Arrange
    var provider = CreateServices(c => { c.RuntimeMode = RuntimeMode.Fallback; });

    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert - Void commands just complete without calling the real handler
    await mediator
      .Invoking(m => m.Send(new CancelOrderCommand { OrderId = Guid.NewGuid() }))
      .Should()
      .NotThrowAsync();
  }

  [Fact]
  public async Task Fallback_Uses_Mock_When_Registered()
  {
    // Arrange - Explicitly mock PlaceOrder, let GetOrderById fall back to default
    var mocked = new OrderResponse { OrderId = Guid.NewGuid(), Status = "Mocked" };

    var provider = CreateServices(c =>
    {
      c.RuntimeMode = RuntimeMode.Fallback;
      c.MockRegistration.Orders.PlaceOrder.WithReturnValue(mocked);
    });

    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var placedOrder = await mediator.Send(new PlaceOrderCommand());
    var fetchedOrder = await mediator.Send(new GetOrderByIdQuery(Guid.NewGuid()));

    // Assert
    placedOrder.Should().BeSameAs(mocked); // Explicit mock used
    fetchedOrder.Should().NotBeNull(); // Fallback creates a default instance
    fetchedOrder!.Status.Should().Be("Pending"); // Default-constructed values
  }
}
