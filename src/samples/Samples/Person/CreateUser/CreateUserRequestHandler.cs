namespace Samples.Person.CreateUser;

using MediatR;

public class CreateUserRequestHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
  public Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
  {
    return Task.FromResult(new CreateUserResponse
    {
      UserId = 123,
      Success = true
    });
  }
}
