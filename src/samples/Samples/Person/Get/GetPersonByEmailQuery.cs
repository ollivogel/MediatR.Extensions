namespace Samples.Person.Get;

using MediatR;
using MediatR.Extensions.Common;

/// <summary>
/// Looks up a person by email address.
///
/// Constructor pattern: PARAMETERLESS constructor only.
/// When there's no constructor with parameters, the Facade Generator
/// uses the parameterless constructor. All method parameters come
/// exclusively from [FacadeParameter] attributes on properties.
///
/// Generated facade: mediator.Persons.Get.ByEmail("john@example.com")
/// </summary>
[MediatorGroup("Persons.Get")]
[MediatorMethodName("ByEmail")]
public class GetPersonByEmailQuery : IRequest<PersonResult?>
{
  /// <summary>
  /// Parameterless constructor — the only one available.
  /// The Facade Generator uses this and relies on FacadeParameters for method params.
  /// </summary>
  public GetPersonByEmailQuery()
  {
  }

  /// <summary>
  /// Required FacadeParameter (non-nullable string).
  /// Appears as a required parameter in the generated facade method.
  /// </summary>
  [FacadeParameter(Order = 0)]
  public string Email { get; set; } = string.Empty;

  /// <summary>
  /// Optional FacadeParameter (nullable bool?).
  /// Automatically becomes optional because it's nullable.
  /// </summary>
  [FacadeParameter(Order = 1)]
  public bool? IncludeInactive { get; set; }
}
