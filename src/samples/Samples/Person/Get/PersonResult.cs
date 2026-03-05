namespace Samples.Person.Get;

/// <summary>
/// Shared result type for person queries.
/// </summary>
public class PersonResult
{
  public Guid Id { get; set; }
  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string? City { get; set; }
}
