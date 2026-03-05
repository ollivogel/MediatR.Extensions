namespace Samples.Tests.Mocking;

using FluentAssertions;
using MediatR;
using MediatR.Extensions.Mocking;
using MediatR.Extensions.Mocking.Registration.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Samples.Orders;
using Xunit;

/// <summary>
/// Tests for advanced mock features:
///
///   .Throws(exception)                  — simulate handler errors
///   .Throws&lt;TException&gt;()         — typed exception shortcut
///   .Returns(Func&lt;TRequest, TResponse&gt;) — dynamic response based on request
///   .ReturnsSequence(...)               — different values on successive calls
/// </summary>
public class AdvancedMockTests
{
  private static ServiceProvider CreateServices(Action<MediatorMockingConfiguration> configure)
  {
    return new ServiceCollection()
      .AddMediatorMocking(configure)
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>())
      .BuildServiceProvider();
  }

  // ================================================================
  //  Throws — simulate handler errors
  // ================================================================

  [Fact]
  public async Task Throws_Exception_On_Request_With_ReturnValue()
  {
    // Arrange — PlaceOrderCommand returns OrderResponse, but we want it to throw
    var provider = CreateServices(c =>
    {
      c.MockRegistration.Orders.PlaceOrder
        .Throws(new InvalidOperationException("Out of stock"));
    });

    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert — The configured exception is thrown
    await mediator
      .Invoking(m => m.Send(new PlaceOrderCommand { ProductName = "Widget" }))
      .Should()
      .ThrowAsync<InvalidOperationException>()
      .WithMessage("Out of stock");
  }

  [Fact]
  public async Task Throws_Generic_Exception()
  {
    // Arrange — Throws<T>() creates a new instance of the exception type
    var provider = CreateServices(c =>
    {
      c.MockRegistration.Orders.PlaceOrder.Throws<TimeoutException>();
    });

    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert
    await mediator
      .Invoking(m => m.Send(new PlaceOrderCommand()))
      .Should()
      .ThrowAsync<TimeoutException>();
  }

  [Fact]
  public async Task Throws_On_Void_Command()
  {
    // Arrange — CancelOrderCommand is IRequest (void), not IRequest<T>
    var provider = CreateServices(c =>
    {
      c.MockRegistration.Orders.CancelOrder
        .Throws(new UnauthorizedAccessException("Not allowed"));
    });

    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert
    await mediator
      .Invoking(m => m.Send(new CancelOrderCommand { OrderId = Guid.NewGuid() }))
      .Should()
      .ThrowAsync<UnauthorizedAccessException>()
      .WithMessage("Not allowed");
  }

  [Fact]
  public async Task Throws_Generic_On_Void_Command()
  {
    var provider = CreateServices(c =>
    {
      c.MockRegistration.Orders.CancelOrder.Throws<TimeoutException>();
    });

    var mediator = provider.GetRequiredService<IMediator>();

    await mediator
      .Invoking(m => m.Send(new CancelOrderCommand { OrderId = Guid.NewGuid() }))
      .Should()
      .ThrowAsync<TimeoutException>();
  }

  [Fact]
  public async Task Throws_Still_Invokes_Callback()
  {
    // Arrange — Callback runs BEFORE the exception is thrown
    PlaceOrderCommand? captured = null;

    var provider = CreateServices(c =>
    {
      c.MockRegistration.Orders.PlaceOrder
        .Throws(new InvalidOperationException("Boom"))
        .WithCallback(request => captured = request);
    });

    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    try
    {
      await mediator.Send(new PlaceOrderCommand { ProductName = "Widget" });
    }
    catch (InvalidOperationException)
    {
      // Expected
    }

    // Assert — Callback was invoked even though handler threw
    captured.Should().NotBeNull();
    captured!.ProductName.Should().Be("Widget");
  }

  // ================================================================
  //  Returns(Func<TRequest, TResponse>) — dynamic response
  // ================================================================

  [Fact]
  public async Task Returns_Dynamic_Response_Based_On_Request()
  {
    // Arrange — Response varies depending on request content
    var provider = CreateServices(c =>
    {
      c.MockRegistration.Orders.PlaceOrder
        .Returns(request => new OrderResponse
        {
          ProductName = request.ProductName,
          Amount = request.Amount,
          Status = request.Amount > 100 ? "NeedsApproval" : "Confirmed"
        });
    });

    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var cheap = await mediator.Send(new PlaceOrderCommand { ProductName = "Pen", Amount = 5.00m });
    var expensive = await mediator.Send(new PlaceOrderCommand { ProductName = "Laptop", Amount = 1500.00m });

    // Assert — Each request got a different response
    cheap.Status.Should().Be("Confirmed");
    cheap.ProductName.Should().Be("Pen");

    expensive.Status.Should().Be("NeedsApproval");
    expensive.ProductName.Should().Be("Laptop");
  }

  [Fact]
  public async Task Returns_Dynamic_With_Callback()
  {
    // Returns(Func<>) and WithCallback() can be combined
    var callCount = 0;

    var provider = CreateServices(c =>
    {
      c.MockRegistration.Orders.PlaceOrder
        .Returns(request => new OrderResponse { Status = $"Order #{callCount}" })
        .WithCallback(_ => callCount++);
    });

    var mediator = provider.GetRequiredService<IMediator>();

    // Act — Callback increments counter before factory is called
    var first = await mediator.Send(new PlaceOrderCommand());
    var second = await mediator.Send(new PlaceOrderCommand());

    // Assert
    first.Status.Should().Be("Order #1");
    second.Status.Should().Be("Order #2");
  }

  // ================================================================
  //  ReturnsSequence — different values on successive calls
  // ================================================================

  [Fact]
  public async Task ReturnsSequence_Cycles_Through_Values()
  {
    // Arrange — Three responses in sequence
    var provider = CreateServices(c =>
    {
      c.MockRegistration.Orders.GetOrderById
        .ReturnsSequence(
          new OrderResponse { Status = "Pending" },
          new OrderResponse { Status = "Processing" },
          new OrderResponse { Status = "Shipped" });
    });

    var mediator = provider.GetRequiredService<IMediator>();
    var orderId = Guid.NewGuid();

    // Act — Each call gets the next value
    var first = await mediator.Send(new GetOrderByIdQuery(orderId));
    var second = await mediator.Send(new GetOrderByIdQuery(orderId));
    var third = await mediator.Send(new GetOrderByIdQuery(orderId));

    // Assert
    first.Status.Should().Be("Pending");
    second.Status.Should().Be("Processing");
    third.Status.Should().Be("Shipped");
  }

  [Fact]
  public async Task ReturnsSequence_Wraps_Around()
  {
    // Arrange — Two values, called three times
    var provider = CreateServices(c =>
    {
      c.MockRegistration.Orders.GetOrderById
        .ReturnsSequence(
          new OrderResponse { Status = "A" },
          new OrderResponse { Status = "B" });
    });

    var mediator = provider.GetRequiredService<IMediator>();
    var orderId = Guid.NewGuid();

    // Act
    var first = await mediator.Send(new GetOrderByIdQuery(orderId));
    var second = await mediator.Send(new GetOrderByIdQuery(orderId));
    var third = await mediator.Send(new GetOrderByIdQuery(orderId));

    // Assert — Third call wraps around to first value
    first.Status.Should().Be("A");
    second.Status.Should().Be("B");
    third.Status.Should().Be("A");
  }

  [Fact]
  public async Task ReturnsSequence_With_Callback()
  {
    var captured = new List<string>();

    var provider = CreateServices(c =>
    {
      c.MockRegistration.Orders.PlaceOrder
        .ReturnsSequence(
          new OrderResponse { Status = "First" },
          new OrderResponse { Status = "Second" })
        .WithCallback(request => captured.Add(request.ProductName));
    });

    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    await mediator.Send(new PlaceOrderCommand { ProductName = "Widget" });
    await mediator.Send(new PlaceOrderCommand { ProductName = "Gadget" });

    // Assert
    captured.Should().Equal("Widget", "Gadget");
  }
}
