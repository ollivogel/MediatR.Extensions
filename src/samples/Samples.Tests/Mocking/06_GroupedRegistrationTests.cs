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
/// [MediatorGroup] organizes mock registrations into a navigable hierarchy.
/// Dot-separated names create nested groups:
///
///   [MediatorGroup("Orders")]          → c.MockRegistration.Orders.PlaceOrder
///   [MediatorGroup("Persons.Admin")]   → c.MockRegistration.Persons.Admin.PersonWithBool
///   [MediatorGroup("Test.Exists")]     → c.MockRegistration.Test.Exists.ByName
///
/// This makes large test setups readable and discoverable via IntelliSense.
/// </summary>
public class GroupedRegistrationTests
{
  [Fact]
  public async Task Single_Level_Group()
  {
    // Arrange
    // All Order requests use [MediatorGroup("Orders")]
    // → Access: c.MockRegistration.Orders.{MethodName}
    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.MockRegistration.Orders.PlaceOrder
          .WithReturnValue(new OrderResponse { Status = "Grouped Mock" });

        c.MockRegistration.Orders.GetOrderById
          .WithReturnValue(new OrderResponse { Status = "Also Grouped" });

        c.MockRegistration.Orders.CancelOrder
          .WithCallback(r => r.Reason.Should().NotBeEmpty());
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>())
      .BuildServiceProvider();

    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var placed = await mediator.Send(new PlaceOrderCommand());
    var fetched = await mediator.Send(new GetOrderByIdQuery(Guid.NewGuid()));
    await mediator.Send(new CancelOrderCommand { Reason = "Test" });

    // Assert
    placed.Status.Should().Be("Grouped Mock");
    fetched.Status.Should().Be("Also Grouped");
  }

  [Fact]
  public async Task Nested_Group_With_Two_Levels()
  {
    // Arrange
    // PersonThatReturnsBoolRequest uses [MediatorGroup("Persons.Admin")]
    // → Access: c.MockRegistration.Persons.Admin.PersonWithBool
    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.RuntimeMode = RuntimeMode.PassThrough;

        // Navigate: MockRegistration → Persons → Admin → PersonWithBool
        c.MockRegistration.Persons.Admin.PersonWithBool
          .ReturnsTrue();
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PersonThatReturnsBoolRequest>())
      .BuildServiceProvider();

    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.Send(new PersonThatReturnsBoolRequest(false));

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public async Task Multiple_Groups_In_Same_Setup()
  {
    // Arrange - Register mocks across different groups in one configuration
    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        // Group: Orders
        c.MockRegistration.Orders.PlaceOrder
          .WithReturnValue(new OrderResponse { Status = "Mocked" });

        // Group: Persons.Admin
        c.MockRegistration.Persons.Admin.PersonWithBool
          .ReturnsFalse();

        // Group: Test.Exists (with custom method name)
        c.MockRegistration.Test.Exists.ByName
          .WithReturnValue(99);
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>())
      .BuildServiceProvider();

    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var order = await mediator.Send(new PlaceOrderCommand());
    var boolResult = await mediator.Send(new PersonThatReturnsBoolRequest());
    var existsResult = await mediator.Send(new TestExistsQuery());

    // Assert
    order.Status.Should().Be("Mocked");
    boolResult.Should().BeFalse();
    existsResult.Should().Be(99);
  }
}
