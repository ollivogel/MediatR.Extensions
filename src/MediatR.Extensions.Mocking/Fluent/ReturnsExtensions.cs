namespace MediatR.Extensions.Mocking.Fluent;

/// <summary>
/// Generic convenience extensions for any response type.
/// These work on all <see cref="IHandlerConfigurationRegistrantFactory{TResponse, TRegistrant}"/>
/// regardless of the response type.
/// </summary>
public static class ReturnsExtensions
{
  extension<TResponse, TRegistrant>(IHandlerConfigurationRegistrantFactory<TResponse, TRegistrant> registrant)
    where TRegistrant : IRegistrant
  {
    /// <summary>
    /// Registers a mock that returns the specified value.
    /// Shorthand for <see cref="IHandlerConfigurationRegistrantFactory{TResponse, TRegistrant}.WithReturnValue"/>.
    /// </summary>
    /// <param name="value">The value to return when the request is sent.</param>
    /// <example>
    /// <code>
    /// c.MockRegistration.Orders.PlaceOrder.Returns(new OrderResponse { Status = "OK" });
    /// c.MockRegistration.Test.Exists.ByName.Returns(42);
    /// c.MockRegistration.Persons.IsActive.Returns(true);
    /// </code>
    /// </example>
    public TRegistrant Returns(TResponse value) => registrant.WithReturnValue(value);

    /// <summary>
    /// Registers a mock that returns <c>default(TResponse)</c>.
    /// For value types this is 0/false/Guid.Empty, for reference types this is null.
    /// </summary>
    /// <example>
    /// <code>
    /// c.MockRegistration.Orders.PlaceOrder.ReturnsDefault();
    /// // Returns null for reference types, 0 for int, false for bool, etc.
    /// </code>
    /// </example>
    public TRegistrant ReturnsDefault() => registrant.WithReturnValue(default!);
  }
}
