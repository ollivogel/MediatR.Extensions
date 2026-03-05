namespace Samples.Person.CreateUser;

using MediatR;
using MediatR.Extensions.Common;

[MediatorGroup("Persons")]
[MediatorMethodName("Create")]
[GenerateFluentBuilder]
public class CreateUserCommand : IRequest<CreateUserResponse>
{
  public CreateUserCommand(string email)
  {
    this.Email = email;
  }

  public CreateUserCommand(string firstname, string email)
  {
    this.Email = email;
    this.FirstName = firstname;
  }

  public CreateUserCommand()
  {
  }

  [BuilderMethod("WithName", Order = 0)] public string FirstName { get; set; } = string.Empty;

  [BuilderMethod("WithName", ParameterName = "thisIsTheLastName", Order = 1)]
  public string LastName { get; set; } = string.Empty;

  public string Email { get; set; } = string.Empty;
}
