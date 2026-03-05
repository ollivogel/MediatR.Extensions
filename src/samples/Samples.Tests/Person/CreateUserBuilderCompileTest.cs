namespace Samples.Tests.Person;

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Verifies that the generated builder code compiles correctly.
/// If the builder was generated, this test compiles and passes.
/// </summary>
public class CreateUserBuilderCompileTest
{
  [Fact]
  public async Task Builder_Compiles_And_Creates_Instance()
  {
    var services = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Samples.Person.CreateUser.CreateUserCommand>())
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    // This line will only compile if the builder was generated successfully
    var builder = mediator.Build.Persons.CreateUserCommand("test@example.com");

    Assert.NotNull(builder);
  }
}
