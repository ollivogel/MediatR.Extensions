namespace Samples.Person.PersonThatReturnsBool;

using MediatR;

public class PersonThatReturnsBoolRequestHandler : IRequestHandler<PersonThatReturnsBoolRequest, bool>
{
  public Task<bool> Handle(PersonThatReturnsBoolRequest request, CancellationToken cancellationToken)
  {
    return Task.FromResult(request.Value);
  }
}
