namespace MediatR.Extensions.Mocking.Fluent;

/// <summary>
/// Convenience extensions for <see cref="Guid"/> response types.
/// </summary>
public static class GuidReturnsExtensions
{
  extension<TRegistrant>(IHandlerConfigurationRegistrantFactory<Guid, TRegistrant> registrant)
    where TRegistrant : IRegistrant
  {
    /// <summary>
    /// Registers a mock that returns a newly generated <see cref="Guid"/>.
    /// The GUID is created once at registration time, not per request.
    /// </summary>
    /// <example>
    /// <code>
    /// c.MockRegistration.Persons.Create.ReturnsNewGuid();
    /// </code>
    /// </example>
    public TRegistrant ReturnsNewGuid() => registrant.WithReturnValue(Guid.NewGuid());
  }
}
