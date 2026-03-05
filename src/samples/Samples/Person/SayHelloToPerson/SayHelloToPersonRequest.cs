namespace Samples.Person.SayHelloToPerson;

using MediatR;
using MediatR.Extensions.Common;

[MediatorGroup("Persons")]
public class SayHelloToPersonRequest : IRequest<SayHelloToPersonResponse>
{
  public string MessageToPerson { get; set; } = string.Empty;
}
