namespace Samples.Person.GetUsers;

public class GetUsersResponse
{
  public List<string> Users { get; set; } = new();
  public int TotalCount { get; set; }
}
