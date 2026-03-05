namespace Samples.Person.Get;

using MediatR;

public class GetPersonByEmailQueryHandler : IRequestHandler<GetPersonByEmailQuery, PersonResult?>
{
  public Task<PersonResult?> Handle(GetPersonByEmailQuery request, CancellationToken cancellationToken)
  {
    return Task.FromResult<PersonResult?>(new PersonResult
    {
      Id = Guid.NewGuid(),
      FirstName = "John",
      LastName = "Doe",
      Email = request.Email
    });
  }
}
