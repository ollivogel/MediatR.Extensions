namespace Samples.Person.Get;

using MediatR;
using MediatR.Extensions.Common;

/// <summary>
/// Searches for persons by name.
///
/// Constructor pattern: TWO constructors with different parameter counts.
/// The Facade Generator picks the "best" constructor — fewest parameters,
/// but not parameterless if others exist. So (string name) wins over
/// (string name, string? country).
///
/// Generated facade: mediator.Persons.Get.ByName("John", city: "Berlin")
/// </summary>
[MediatorGroup("Persons.Get")]
[MediatorMethodName("ByName")]
public class GetPersonByNameQuery : IRequest<PersonResult>
{
  /// <summary>
  /// Minimal constructor — this one gets picked by the Facade Generator
  /// because it has the fewest parameters (but is not parameterless).
  /// </summary>
  public GetPersonByNameQuery(string name)
  {
    Name = name;
  }

  /// <summary>
  /// Extended constructor with nullable parameter.
  /// The Facade Generator ignores this one (it picks the smallest).
  /// The FluentBuilder Generator supports ALL public constructors.
  /// </summary>
  public GetPersonByNameQuery(string name, string? country)
  {
    Name = name;
    Country = country;
  }

  public string Name { get; }

  public string? Country { get; set; }

  /// <summary>
  /// Optional city filter — a [FacadeParameter] on a nullable property
  /// becomes an optional parameter with null default in the generated facade.
  /// </summary>
  [FacadeParameter(Order = 0)]
  public string? City { get; set; }
}
