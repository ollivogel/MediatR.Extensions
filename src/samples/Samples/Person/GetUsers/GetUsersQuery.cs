namespace Samples.Person.GetUsers;

using MediatR;
using MediatR.Extensions.Common;

[MediatorGroup("Persons")]
[MediatorMethodName("GetUsers")]
public class GetUsersQuery : IRequest<GetUsersResponse>
{
  [FacadeParameter(Order = 0)] public int PageNumber { get; set; }

  [FacadeParameter(Order = 1, IsOptional = true, DefaultValue = 10)]
  public int PageSize { get; set; }

  [FacadeParameter(Order = 2)] public string? SearchTerm { get; set; }

  public GetUsersQuery(string tenantId)
  {
    this.TenantId = tenantId;
  }

  public string TenantId { get; set; }
}
