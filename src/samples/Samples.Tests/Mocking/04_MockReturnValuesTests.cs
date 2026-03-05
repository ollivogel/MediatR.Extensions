namespace Samples.Tests.Mocking;

using FluentAssertions;
using MediatR;
using MediatR.Extensions.Mocking;
using MediatR.Extensions.Mocking.Fluent;
using MediatR.Extensions.Mocking.Registration.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Samples.Orders;
using Samples.Person.PersonThatReturnsBool;
using Samples.Person.CreateUser;
using Xunit;

/// <summary>
/// Mock Registration provides a fluent API to define return values for mocked requests.
///
/// For requests returning complex types:  .WithReturnValue(response)
/// For bool:                              .ReturnsTrue() / .ReturnsFalse()
/// For int:                               .ReturnsInt(42)
/// For Guid:                              .ReturnsGuid()
/// </summary>
public class MockReturnValuesTests
{
  [Fact]
  public async Task WithReturnValue_Returns_Complex_Object()
  {
    // Arrange
    var expected = new OrderResponse
    {
      OrderId = Guid.NewGuid(),
      ProductName = "Mocked Widget",
      Amount = 0.00m,
      Status = "TestStatus",
    };

    var provider = new ServiceCollection()
      .AddMediatorMocking(c => { c.MockRegistration.Orders.PlaceOrder.WithReturnValue(expected); })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>())
      .BuildServiceProvider();

    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.Send(new PlaceOrderCommand { ProductName = "Anything" });

    // Assert - Always returns the same mocked instance
    result.Should().BeSameAs(expected);
    result.Status.Should().Be("TestStatus");
  }

  [Fact]
  public async Task ReturnsTrue_Convenience_Method()
  {
    // Arrange - PersonThatReturnsBoolRequest returns IRequest<bool>
    var provider = new ServiceCollection()
      .AddMediatorMocking(c => { c.MockRegistration.Persons.Admin.PersonWithBool.ReturnsTrue(); })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PersonThatReturnsBoolRequest>())
      .BuildServiceProvider();

    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.Send(new PersonThatReturnsBoolRequest(false));

    // Assert - Returns true regardless of input
    result.Should().BeTrue();
  }

  [Fact]
  public async Task ReturnsInt_For_Integer_Responses()
  {
    // Arrange - TestExistsQuery returns IRequest<int>
    var provider = new ServiceCollection()
      .AddMediatorMocking(c => { c.MockRegistration.Test.Exists.ByName.WithReturnValue(42); })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PersonThatReturnsBoolRequest>())
      .BuildServiceProvider();

    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.Send(new TestExistsQuery());

    // Assert
    result.Should().Be(42);
  }

  [Fact]
  public async Task Void_Commands_Register_Without_Return_Value()
  {
    // Arrange - CancelOrderCommand implements IRequest (no return value)
    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        // For void commands, simply access the registrant - no WithReturnValue needed
        _ = c.MockRegistration.Orders.CancelOrder;
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CancelOrderCommand>())
      .BuildServiceProvider();

    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert - Doesn't throw, mock intercepts before real handler
    await mediator
      .Invoking(m => m.Send(new CancelOrderCommand { OrderId = Guid.NewGuid() }))
      .Should()
      .NotThrowAsync();
  }
}
