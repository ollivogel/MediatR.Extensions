namespace Samples.Person.Delete;

using MediatR;
using MediatR.Extensions.Common;

/// <summary>
/// Deletes a person by ID.
///
/// Constructor pattern: MIXED types — Guid + bool.
/// Demonstrates that constructor parameters of different types (value types,
/// reference types, nullable) are all supported.
///
/// Generated facade: mediator.Persons.Delete(id, permanent)
///
/// Note: Both constructor parameters become required in the facade, even though
/// in many real-world scenarios "permanent" might have a default. Constructor
/// parameter defaults are not preserved by the generator — use [FacadeParameter]
/// with IsOptional/DefaultValue for optional parameters instead.
/// </summary>
[MediatorGroup("Persons")]
[MediatorMethodName("Delete")]
public class DeletePersonCommand : IRequest
{
  /// <summary>
  /// Constructor with two required parameters of different types.
  /// </summary>
  public DeletePersonCommand(Guid id, bool permanent)
  {
    Id = id;
    Permanent = permanent;
  }

  public Guid Id { get; }

  public bool Permanent { get; }
}
