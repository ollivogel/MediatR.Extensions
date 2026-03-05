namespace Samples.Tests.Mocking;

using FluentAssertions;
using MediatR;
using MediatR.Extensions.Mocking;
using MediatR.Extensions.Mocking.Registration.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Samples.Orders;
using Xunit;

/// <summary>
/// Callbacks let you inspect and assert on the request object that was sent.
/// They are invoked BEFORE the mocked response is returned.
///
/// This is useful for verifying that your code under test sends the correct data.
/// </summary>
public class CallbackTests
{
  [Fact]
  public async Task Callback_Inspects_Request_With_Return_Value()
  {
    // Arrange
    PlaceOrderCommand? capturedRequest = null;

    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.MockRegistration.Orders.PlaceOrder
          .WithReturnValue(new OrderResponse { Status = "OK" })
          .WithCallback(request =>
          {
            // Capture the request for later assertion
            capturedRequest = request;
          });
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>())
      .BuildServiceProvider();

    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    await mediator.Send(new PlaceOrderCommand { ProductName = "Deluxe Widget", Amount = 99.99m });

    // Assert - Verify the request was sent with correct data
    capturedRequest.Should().NotBeNull();
    capturedRequest!.ProductName.Should().Be("Deluxe Widget");
    capturedRequest.Amount.Should().Be(99.99m);
  }

  [Fact]
  public async Task Callback_Inspects_Void_Commands()
  {
    // Arrange
    var expectedOrderId = Guid.NewGuid();
    CancelOrderCommand? capturedRequest = null;

    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.MockRegistration.Orders.CancelOrder
          .WithCallback(request => capturedRequest = request);
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CancelOrderCommand>())
      .BuildServiceProvider();

    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    await mediator.Send(new CancelOrderCommand { OrderId = expectedOrderId, Reason = "Out of stock" });

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.OrderId.Should().Be(expectedOrderId);
    capturedRequest.Reason.Should().Be("Out of stock");
  }

  [Fact]
  public async Task Callback_Can_Assert_Inline()
  {
    // Arrange - Assert directly inside the callback using FluentAssertions
    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.MockRegistration.Orders.PlaceOrder
          .WithReturnValue(new OrderResponse())
          .WithCallback(request =>
          {
            request.ProductName.Should().Be("Expected Product");
            request.Amount.Should().BeGreaterThan(0);
          });
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>())
      .BuildServiceProvider();

    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert - Callback assertion passes
    await mediator.Send(new PlaceOrderCommand { ProductName = "Expected Product", Amount = 10.00m });
  }

  [Fact]
  public async Task Callback_Failure_Propagates_As_Test_Failure()
  {
    // Arrange - Callback assertion that will fail
    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.MockRegistration.Orders.PlaceOrder
          .WithReturnValue(new OrderResponse())
          .WithCallback(request =>
          {
            // This will throw because "Wrong Name" != "Widget"
            request.ProductName.Should().Be("Widget");
          });
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>())
      .BuildServiceProvider();

    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert - The callback's assertion failure bubbles up
    await mediator
      .Invoking(m => m.Send(new PlaceOrderCommand { ProductName = "Wrong Name" }))
      .Should()
      .ThrowAsync<Exception>();
  }
}
