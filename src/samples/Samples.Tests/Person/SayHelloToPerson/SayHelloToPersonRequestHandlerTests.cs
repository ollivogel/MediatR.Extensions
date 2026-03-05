namespace Samples.Tests.Person.SayHelloToPerson;

using FluentAssertions;
using MediatR;
using MediatR.Extensions.Mocking.Registration.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Samples.Person.SayHelloToPerson;
using Samples.Person.SayHelloToPersonWithoutReturnValue;
using Xunit;

public class SayHelloToPersonRequestHandlerTests
{
  [Fact]
  public async Task Mock_Intercepts_Both_Void_And_ReturnValue_Requests()
  {
    var a1 = typeof(SayHelloToPersonWithoutReturnValueRequest).Assembly;
    var a2 = typeof(SayHelloToPersonRequest).Assembly;

    var services = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.MockRegistration.Persons.SayHelloToPersonWithoutReturnValue
          .WithCallback(request => request.Message.Should().Be("Hello World"));

        c.MockRegistration.Persons.SayHelloToPerson
          .WithReturnValue(new SayHelloToPersonResponse { Response = "Mock Response" })
          .WithCallback(request => request.MessageToPerson.Should().Be("Message To Person"));
      })
      .AddMediatR(c => { c.RegisterServicesFromAssemblies(a1, a2); })
      .BuildServiceProvider();

    var mediator = services.GetRequiredService<IMediator>();

    await mediator.Send(new SayHelloToPersonWithoutReturnValueRequest { Message = "Hello World" });
    var response = await mediator.Send(new SayHelloToPersonRequest { MessageToPerson = "Message To Person" });

    response.Response.Should().Be("Mock Response");
  }
}
