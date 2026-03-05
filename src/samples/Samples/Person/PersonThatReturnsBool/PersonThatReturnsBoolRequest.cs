namespace Samples.Person.PersonThatReturnsBool;

using MediatR;
using MediatR.Extensions.Common;

[MediatorGroup("Persons.Admin")]
[MediatorMethodName("PersonWithBool")]
public class PersonThatReturnsBoolRequest : IRequest<bool>
{
  public PersonThatReturnsBoolRequest()
  {
  }

  public PersonThatReturnsBoolRequest(bool value)
  {
    this.Value = value;
  }

  public bool Value { get; set; }
}
