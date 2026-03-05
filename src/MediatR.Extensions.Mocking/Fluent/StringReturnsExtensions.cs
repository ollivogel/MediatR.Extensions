namespace MediatR.Extensions.Mocking.Fluent;

/// <summary>
/// Convenience extensions for <see cref="string"/> response types.
/// </summary>
public static class StringReturnsExtensions
{
  extension<TRegistrant>(IHandlerConfigurationRegistrantFactory<string, TRegistrant> registrant)
    where TRegistrant : IRegistrant
  {
    /// <summary>
    /// Registers a mock that returns <see cref="string.Empty"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// c.MockRegistration.Persons.GetName.ReturnsEmpty();
    /// </code>
    /// </example>
    public TRegistrant ReturnsEmpty() => registrant.WithReturnValue(string.Empty);
  }
}
