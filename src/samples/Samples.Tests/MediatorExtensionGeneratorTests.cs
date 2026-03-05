namespace Samples.Tests;

using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Samples.Person.SayHelloToPerson;
using Samples.Person.SayHelloToPersonWithoutReturnValue;
using Xunit;

public class MediatorExtensionGeneratorTests
{
  [Fact]
  public async Task Facade_Works_For_Request_With_Return_Value()
  {
    var a1 = typeof(SayHelloToPersonWithoutReturnValueRequest).Assembly;
    var a2 = typeof(SayHelloToPersonRequest).Assembly;

    var services = new ServiceCollection()
      .AddMediatR(c => { c.RegisterServicesFromAssemblies(a1, a2); })
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();
    var result = await mediator.Persons.Admin.PersonWithBool(false);

    result.Should().BeFalse();
  }
}
