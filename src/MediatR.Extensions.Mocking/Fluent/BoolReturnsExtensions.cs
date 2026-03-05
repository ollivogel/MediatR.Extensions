namespace MediatR.Extensions.Mocking.Fluent;

/// <summary>
/// Convenience extensions for <see cref="bool"/> response types.
/// </summary>
public static class BoolReturnsExtensions
{
  extension<TRegistrant>(IHandlerConfigurationRegistrantFactory<bool, TRegistrant> registrant)
    where TRegistrant : IRegistrant
  {
    /// <summary>
    /// Registers a mock that returns <c>true</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// c.MockRegistration.Persons.IsActive.ReturnsTrue();
    /// </code>
    /// </example>
    public TRegistrant ReturnsTrue() => registrant.WithReturnValue(true);

    /// <summary>
    /// Registers a mock that returns <c>false</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// c.MockRegistration.Persons.IsActive.ReturnsFalse();
    /// </code>
    /// </example>
    public TRegistrant ReturnsFalse() => registrant.WithReturnValue(false);
  }
}
