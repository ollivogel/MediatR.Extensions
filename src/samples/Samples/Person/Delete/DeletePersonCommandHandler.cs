namespace Samples.Person.Delete;

using MediatR;

public class DeletePersonCommandHandler : IRequestHandler<DeletePersonCommand>
{
  public Task Handle(DeletePersonCommand request, CancellationToken cancellationToken)
  {
    // In a real app: soft-delete or permanent delete based on request.Permanent
    return Task.CompletedTask;
  }
}
