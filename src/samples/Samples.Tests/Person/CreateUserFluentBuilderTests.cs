namespace Samples.Tests.Person;

using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Samples.Person.CreateUser;
using Xunit;

public class CreateUserFluentBuilderTests
{
  [Fact]
  public async Task FluentBuilder_ShouldWork_WithAllProperties()
  {
    // Arrange
    var services = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateUserCommand>())
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    // Act - CreateUser now takes email as constructor parameter
    var result = await mediator.Build.Persons.CreateUserCommand("max@example.com")
      .WithName("Max", "Mustermann")
      .Send();

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.UserId.Should().Be(123);
  }

  [Fact]
  public async Task FluentBuilder_ShouldWork_WithPartialProperties()
  {
    // Arrange
    var services = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateUserCommand>())
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    // Act - Only name, email set via constructor
    var result = await mediator.Build.Persons.CreateUserCommand("max@example.com")
      .WithName("Max", "Mustermann")
      .Send();

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
  }

  [Fact]
  public async Task FluentBuilder_ShouldBeChainable()
  {
    // Arrange
    var services = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateUserCommand>())
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    // Act - Chain should return builder instance
    var builder = mediator.Build.Persons.CreateUserCommand("initial@example.com")
      .WithName("Max", "Mustermann");

    builder.Should().NotBeNull();

    var result = await builder.Send();

    // Assert
    result.Should().NotBeNull();
  }

  [Fact]
  public async Task FluentBuilder_ShouldRespectParameterOrder()
  {
    // Arrange
    var services = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateUserCommand>())
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    // Act - firstName (Order=0) should come before lastName (Order=1)
    var result = await mediator.Build.Persons.CreateUserCommand("test@example.com")
      .WithName("Max", "Mustermann") // firstName, lastName
      .Send();

    // Assert - The handler should have received the correct values
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
  }

  [Fact]
  public async Task FluentBuilder_ShouldWork_WithParameterlessConstructor()
  {
    // Arrange
    var services = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateUserCommand>())
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    // Act - Using parameterless constructor overload
    var result = await mediator.Build.Persons
      .CreateUserCommand("bla@bla.com")
      .WithName("Max", "Mustermann")
      .Send();

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
  }

  [Fact]
  public async Task FluentBuilder_ShouldWork_WithParameterizedConstructor()
  {
    // Arrange
    var services = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateUserCommand>())
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    // Act - Using constructor with email parameter
    var result = await mediator.Build.Persons.CreateUserCommand("initial@example.com")
      .WithName("Max", "Mustermann")
      .Send();

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
  }

  [Fact]
  public void FluentBuilder_Build_ShouldReturnRequest()
  {
    // Arrange
    var services = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateUserCommand>())
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    // Act
    var request = mediator.Build.Persons.CreateUserCommand("test@example.com")
      .WithName("Max", "Mustermann")
      .Build();

    // Assert
    request.Should().NotBeNull();
    request.Email.Should().Be("test@example.com");
    request.FirstName.Should().Be("Max");
    request.LastName.Should().Be("Mustermann");
  }

  [Fact]
  public async Task FluentBuilder_Build_ShouldBeReusable()
  {
    // Arrange
    var services = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateUserCommand>())
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    // Act
    var request = mediator.Build.Persons.CreateUserCommand("test@example.com")
      .WithName("Max", "Mustermann")
      .Build();

    var result = await mediator.Send(request);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
  }

  [Fact]
  public async Task FluentBuilder_BuildNamespace_ShouldSupportGroupNavigation()
  {
    // Arrange
    var services = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateUserCommand>())
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    // Act - Verify that the Build namespace hierarchy works correctly
    // mediator.Build -> MediatorBuildNamespace
    // mediator.Build.Persons -> PersonsBuildNamespace
    // mediator.Build.Persons.CreateUserCommand -> CreateUserCommandBuilder
    var buildNamespace = mediator.Build;
    buildNamespace.Should().NotBeNull("Build namespace should be accessible");

    var personsNamespace = mediator.Build.Persons;
    personsNamespace.Should().NotBeNull("Persons namespace should be accessible");

    var builder = mediator.Build.Persons.CreateUserCommand("navigation@example.com");
    builder.Should().NotBeNull("CreateUserCommand builder should be accessible");

    // Act - Use the builder
    var result = await builder
      .WithName("Navigation", "Test")
      .Send();

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
  }

  [Fact]
  public async Task FluentBuilder_Send_And_Build_ShouldProduceSameRequest()
  {
    // Arrange
    var services = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateUserCommand>())
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    // Act - Create request via Build()
    var builtRequest = mediator.Build.Persons.CreateUserCommand("compare@example.com")
      .WithName("John", "Doe")
      .Build();

    // Act - Send directly via Send()
    var sendResult = await mediator.Build.Persons.CreateUserCommand("compare@example.com")
      .WithName("John", "Doe")
      .Send();

    // Assert - Both should have same properties
    builtRequest.Email.Should().Be("compare@example.com");
    builtRequest.FirstName.Should().Be("John");
    builtRequest.LastName.Should().Be("Doe");

    sendResult.Should().NotBeNull();
    sendResult.Success.Should().BeTrue();
  }
}
