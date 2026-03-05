namespace Samples.Person.Update;

using MediatR;

public class UpdatePersonCommandHandler : IRequestHandler<UpdatePersonCommand, UpdatePersonResult>
{
  public Task<UpdatePersonResult> Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
  {
    return Task.FromResult(new UpdatePersonResult
    {
      Success = true,
      Message = $"Updated person {request.Id}: {request.FirstName} {request.LastName}"
                + (request.Email is not null ? $", email={request.Email}" : "")
                + (request.Phone is not null ? $", phone={request.Phone}" : "")
    });
  }
}
