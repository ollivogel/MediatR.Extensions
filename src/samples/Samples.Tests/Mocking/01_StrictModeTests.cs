namespace Samples.Tests.Mocking;

using FluentAssertions;
using MediatR;
using MediatR.Extensions.Mocking;
using MediatR.Extensions.Mocking.Registration.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Samples.Orders;
using Xunit;

/// <summary>
/// Strict Mode is the default. Every request MUST have a mock registered,
/// otherwise an InvalidOperationException is thrown.
///
/// Use this when you want to be explicit about what your code under test calls.
/// </summary>
public class StrictModeTests
{
  private static ServiceProvider CreateServices(Action<MediatorMockingConfiguration> configure)
  {
    return new ServiceCollection()
      .AddMediatorMocking(configure)
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>())
      .BuildServiceProvider();
  }

  [Fact]
  public async Task Strict_Throws_When_No_Mock_Registered()
  {
    // Arrange - No mocks registered, Strict is the default
    var provider = CreateServices(_ => { });
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert - Strict mode throws for unmocked requests
    await mediator
      .Invoking(m => m.Send(new PlaceOrderCommand { ProductName = "Widget", Amount = 9.99m }))
      .Should()
      .ThrowAsync<InvalidOperationException>();
  }

  [Fact]
  public async Task Strict_Throws_For_Requests_Without_Return_Value()
  {
    // Arrange
    var provider = CreateServices(_ => { });
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert - Also throws for IRequest (void) commands
    await mediator
      .Invoking(m => m.Send(new CancelOrderCommand { OrderId = Guid.NewGuid() }))
      .Should()
      .ThrowAsync<InvalidOperationException>();
  }

  [Fact]
  public async Task Strict_Works_When_Mock_Is_Registered()
  {
    // Arrange - Register a mock for PlaceOrderCommand
    var expected = new OrderResponse { OrderId = Guid.NewGuid(), Status = "Mocked" };

    var provider = CreateServices(c =>
    {
      // RuntimeMode.Strict is the default, no need to set it explicitly
      c.MockRegistration.Orders.PlaceOrder.WithReturnValue(expected);
    });

    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.Send(new PlaceOrderCommand { ProductName = "Widget" });

    // Assert - Returns the mocked response, real handler is never called
    result.Should().BeSameAs(expected);
    result.Status.Should().Be("Mocked");
  }
}
