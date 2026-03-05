namespace Samples.Tests.Person;

using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Samples.Person.GetUsers;
using Xunit;

public class FacadeParameterTests
{
  [Fact]
  public async Task GetUsers_WithAllParameters_ShouldWork()
  {
    // Arrange
    var services = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUsersQuery>())
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    // Act - Call with all parameters (tenantId from constructor, pageNumber, pageSize, searchTerm from FacadeParameter)
    var result = await mediator.Persons.GetUsers("tenant-123", 1, 20, "john");

    // Assert
    result.Should().NotBeNull();
    result.Users.Should().NotBeEmpty();
  }

  [Fact]
  public async Task GetUsers_WithOptionalParameterOmitted_ShouldUseDefaultValue()
  {
    // Arrange
    var services = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUsersQuery>())
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    // Act - Omit optional parameters (pageSize should default to 10, searchTerm to null)
    var result = await mediator.Persons.GetUsers("tenant-123", 1);

    // Assert
    result.Should().NotBeNull();
    result.Users.Should().NotBeEmpty();
  }

  [Fact]
  public async Task GetUsers_WithConfigureAction_ShouldAllowFurtherConfiguration()
  {
    // Arrange
    var services = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUsersQuery>())
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    // Act - Use configure action to verify parameters
    GetUsersQuery? capturedQuery = null;
    var result = await mediator.Persons.GetUsers("tenant-123", 1, 20, "john", configure: q => { capturedQuery = q; });

    // Assert
    capturedQuery.Should().NotBeNull();
    capturedQuery!.TenantId.Should().Be("tenant-123");
    capturedQuery.PageNumber.Should().Be(1);
    capturedQuery.PageSize.Should().Be(20);
    capturedQuery.SearchTerm.Should().Be("john");
  }
}
