namespace Samples.Tests.Person;

using FluentAssertions;
using MediatR;
using MediatR.Extensions.Mocking;
using MediatR.Extensions.Mocking.Fluent;
using MediatR.Extensions.Mocking.Registration.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Samples.Person.PersonThatReturnsBool;
using Samples.Person.SayHelloToPersonWithoutReturnValue;
using Xunit;

public class MockNewMediatorTests
{
  // Pass Through
  [Fact]
  public async Task Works_With_Return_Value_In_PassThroughMode()
  {
    Type[] markerTypes = [typeof(PersonThatReturnsBoolRequest)];
    var assemblies = markerTypes.Select(d => d.Assembly).Distinct().ToArray();

    var services = new ServiceCollection()
      .AddMediatorMocking(c => { c.RuntimeMode = RuntimeMode.PassThrough; })
      .AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(assemblies))
      .BuildServiceProvider();

    var mn = services.GetRequiredService<IMediator>();
    var request = new PersonThatReturnsBoolRequest()
    {
      Value = true,
    };
    var result = await mn.Send(request);
    result.Should().BeTrue();
  }

  [Fact]
  public async Task Works_Without_Return_Value_In_PassThroughMode()
  {
    Type[] markerTypes = [typeof(PersonThatReturnsBoolRequest)];
    var assemblies = markerTypes.Select(d => d.Assembly).Distinct().ToArray();

    var services = new ServiceCollection()
      .AddMediatorMocking(c => { c.RuntimeMode = RuntimeMode.PassThrough; })
      .AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(assemblies))
      .BuildServiceProvider();

    var mn = services.GetRequiredService<IMediator>();
    var request = new SayHelloToPersonWithoutReturnValueRequest
    {
      Message = "Testme",
    };

    await mn.Send(request);
  }

  // Strict Mode

  [Fact]
  public async Task Works_With_Return_Value_In_StrictMode()
  {
    Type[] markerTypes = [typeof(PersonThatReturnsBoolRequest)];
    var assemblies = markerTypes.Select(d => d.Assembly).Distinct().ToArray();

    var services = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.RuntimeMode = RuntimeMode.PassThrough;
        c.MockRegistration.Persons.Admin.PersonWithBool.ReturnsTrue().WithCallback(r => r.Value.Should().BeFalse());
        c.MockRegistration.Test.Exists.ByName.WithReturnValue(12);
        c.MockRegistration.AnotherTest.Exists.ByName.WithReturnValue(12);
      })
      .AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(assemblies))
      .BuildServiceProvider();

    var mn = services.GetRequiredService<IMediator>();
    var request = new PersonThatReturnsBoolRequest
    {
      Value = false,
    };
    var result = await mn.Send(request);
    result.Should().BeTrue();
  }

  [Fact]
  public async Task Works_Without_Return_Value_In_StrictMode()
  {
    Type[] markerTypes = [typeof(PersonThatReturnsBoolRequest)];
    var assemblies = markerTypes.Select(d => d.Assembly).Distinct().ToArray();

    var services = new ServiceCollection()
      .AddMediatorMocking(c =>
      {
        c.RuntimeMode = RuntimeMode.PassThrough;
        c.MockRegistration.Persons.SayHelloToPersonWithoutReturnValue
          .WithCallback(valueRequest => valueRequest.Message.Should().Be("Testme"));
      })
      .AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(assemblies))
      .BuildServiceProvider();

    var mn = services.GetRequiredService<IMediator>();
    var request = new SayHelloToPersonWithoutReturnValueRequest
    {
      Message = "Testme",
    };

    await mn.Send(request);
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task Should_Throw_In_Strict_Mode_Test(bool hasReturnValue)
  {
    var services = new ServiceCollection()
      .AddMediatorMocking(_ => { })
      .AddMediatR(c => c.RegisterServicesFromAssemblyContaining(this.GetType()))
      .BuildServiceProvider();

    var mn = services.GetRequiredService<IMediator>();

    IBaseRequest request = hasReturnValue
      ? new PersonThatReturnsBoolRequest
      {
        Value = true,
      }
      : new SayHelloToPersonWithoutReturnValueRequest();

    await mn
      .Invoking(async h => { await h.Send(request, CancellationToken.None); }).Should()
      .ThrowAsync<InvalidOperationException>();
  }

  // Fallback Mode

  [Fact]
  public async Task Works_Without_Return_Value_In_FallbackMode()
  {
    var services = new ServiceCollection()
      .AddMediatorMocking(c => { c.RuntimeMode = RuntimeMode.Fallback; })
      .AddMediatR(c => c.RegisterServicesFromAssemblyContaining(this.GetType()))
      .BuildServiceProvider();

    var mn = services.GetRequiredService<IMediator>();

    var request = new SayHelloToPersonWithoutReturnValueRequest
    {
      Message = "Testme",
    };

    await mn.Send(request);
  }

  [Fact]
  public async Task Works_With_Return_Value_In_FallbackMode()
  {
    var services = new ServiceCollection()
      .AddMediatorMocking(c => { c.RuntimeMode = RuntimeMode.Fallback; })
      .AddMediatR(c => c.RegisterServicesFromAssemblyContaining(this.GetType()))
      .BuildServiceProvider();

    var mn = services.GetRequiredService<IMediator>();


    var request = new PersonThatReturnsBoolRequest
    {
      Value = false,
    };
    var result = await mn.Send(request);
    result.Should().BeFalse();
  }
}
