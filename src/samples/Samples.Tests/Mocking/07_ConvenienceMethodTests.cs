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
/// Tests for the fluent convenience methods on mock registrations.
///
/// Instead of always writing .WithReturnValue(value), you can use:
///   .Returns(value)         — generic shorthand for any type
///   .ReturnsDefault()       — returns default(TResponse)
///   .ReturnsTrue/False()    — bool shortcuts
///   .ReturnsEmpty()         — string shortcut
///   .ReturnsNewGuid()       — Guid shortcut
///   .ReturnsNow/UtcNow()   — DateTime/DateTimeOffset shortcuts
/// </summary>
public class ConvenienceMethodTests
{
  // ================================================================
  //  Generic Returns() — works for any type
  // ================================================================

  [Fact]
  public async Task Returns_Complex_Object()
  {
    var expected = new OrderResponse { Status = "Via Returns" };

    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.MockRegistration.Orders.PlaceOrder.Returns(expected);
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>())
      .BuildServiceProvider();

    var result = await provider.GetRequiredService<IMediator>()
      .Send(new PlaceOrderCommand { ProductName = "Widget" });

    result.Should().BeSameAs(expected);
  }

  [Fact]
  public async Task Returns_Int()
  {
    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.MockRegistration.Test.Exists.ByName.Returns(42);
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<TestExistsQuery>())
      .BuildServiceProvider();

    var result = await provider.GetRequiredService<IMediator>()
      .Send(new TestExistsQuery());

    result.Should().Be(42);
  }

  [Fact]
  public async Task Returns_Bool()
  {
    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.MockRegistration.Persons.Admin.PersonWithBool.Returns(true);
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PersonThatReturnsBoolRequest>())
      .BuildServiceProvider();

    var result = await provider.GetRequiredService<IMediator>()
      .Send(new PersonThatReturnsBoolRequest(false));

    result.Should().BeTrue();
  }

  // ================================================================
  //  ReturnsDefault() — returns default(TResponse)
  // ================================================================

  [Fact]
  public async Task ReturnsDefault_Int_Is_Zero()
  {
    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.MockRegistration.Test.Exists.ByName.ReturnsDefault();
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<TestExistsQuery>())
      .BuildServiceProvider();

    var result = await provider.GetRequiredService<IMediator>()
      .Send(new TestExistsQuery());

    result.Should().Be(0);
  }

  [Fact]
  public async Task ReturnsDefault_Bool_Is_False()
  {
    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.MockRegistration.Persons.Admin.PersonWithBool.ReturnsDefault();
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PersonThatReturnsBoolRequest>())
      .BuildServiceProvider();

    var result = await provider.GetRequiredService<IMediator>()
      .Send(new PersonThatReturnsBoolRequest(true));

    result.Should().BeFalse();
  }

  // ================================================================
  //  Bool: ReturnsTrue() / ReturnsFalse()
  // ================================================================

  [Fact]
  public async Task ReturnsTrue()
  {
    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.MockRegistration.Persons.Admin.PersonWithBool.ReturnsTrue();
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PersonThatReturnsBoolRequest>())
      .BuildServiceProvider();

    var result = await provider.GetRequiredService<IMediator>()
      .Send(new PersonThatReturnsBoolRequest(false));

    result.Should().BeTrue();
  }

  [Fact]
  public async Task ReturnsFalse()
  {
    var provider = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.MockRegistration.Persons.Admin.PersonWithBool.ReturnsFalse();
      })
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PersonThatReturnsBoolRequest>())
      .BuildServiceProvider();

    var result = await provider.GetRequiredService<IMediator>()
      .Send(new PersonThatReturnsBoolRequest(true));

    result.Should().BeFalse();
  }
}
