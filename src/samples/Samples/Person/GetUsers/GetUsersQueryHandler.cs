namespace Samples.Person.GetUsers;

using MediatR;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, GetUsersResponse>
{
  public Task<GetUsersResponse> Handle(GetUsersQuery request, CancellationToken cancellationToken)
  {
    var response = new GetUsersResponse
    {
      Users = new List<string> { $"User from tenant {request.TenantId}" },
      TotalCount = 1
    };

    return Task.FromResult(response);
  }
}
