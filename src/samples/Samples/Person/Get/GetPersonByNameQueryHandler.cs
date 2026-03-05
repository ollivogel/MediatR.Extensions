namespace Samples.Person.Get;

using MediatR;

public class GetPersonByNameQueryHandler : IRequestHandler<GetPersonByNameQuery, PersonResult>
{
  public Task<PersonResult> Handle(GetPersonByNameQuery request, CancellationToken cancellationToken)
  {
    return Task.FromResult(new PersonResult
    {
      Id = Guid.NewGuid(),
      FirstName = request.Name,
      LastName = "Doe",
      Email = $"{request.Name.ToLower()}@example.com",
      City = request.City
    });
  }
}
