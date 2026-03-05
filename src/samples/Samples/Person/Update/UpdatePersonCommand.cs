namespace Samples.Person.Update;

using MediatR;
using MediatR.Extensions.Common;

/// <summary>
/// Updates a person's profile.
///
/// Constructor pattern: MULTIPLE required parameters (Guid + string + string).
/// All constructor parameters become required leading parameters in the facade.
/// After them come the optional FacadeParameters.
///
/// Generated facade:
///   mediator.Persons.Update(id, "John", "Doe", email: "john@example.com", phone: "+49...")
/// </summary>
[MediatorGroup("Persons")]
[MediatorMethodName("Update")]
public class UpdatePersonCommand : IRequest<UpdatePersonResult>
{
  /// <summary>
  /// Constructor with three required parameters.
  /// All three become required leading parameters in the generated facade.
  /// </summary>
  public UpdatePersonCommand(Guid id, string firstName, string lastName)
  {
    Id = id;
    FirstName = firstName;
    LastName = lastName;
  }

  public Guid Id { get; }
  public string FirstName { get; }
  public string LastName { get; }

  /// <summary>
  /// Optional — nullable string becomes an optional FacadeParameter with null default.
  /// </summary>
  [FacadeParameter(Order = 0)]
  public string? Email { get; set; }

  /// <summary>
  /// Optional — another nullable FacadeParameter.
  /// </summary>
  [FacadeParameter(Order = 1)]
  public string? Phone { get; set; }
}
