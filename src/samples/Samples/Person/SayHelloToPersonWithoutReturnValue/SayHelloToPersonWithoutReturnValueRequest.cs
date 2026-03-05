namespace Samples.Person.SayHelloToPersonWithoutReturnValue;

using MediatR;
using MediatR.Extensions.Common;

[MediatorGroup("Persons")]
public class SayHelloToPersonWithoutReturnValueRequest : IRequest
{
  public string Message { get; set; } = string.Empty;
}
