namespace MediatR.Extensions.Mocking.Fluent;

/// <summary>
/// Factory interface implemented by generated mock registration classes.
/// Provides the <see cref="WithReturnValue"/> method and serves as the extension point
/// for convenience methods like <c>ReturnsTrue()</c>, <c>ReturnsDefault()</c>, etc.
/// </summary>
/// <typeparam name="TResponse">The response type of the mocked handler.</typeparam>
/// <typeparam name="TRegistrant">The generated registrant type returned for further chaining.</typeparam>
public interface IHandlerConfigurationRegistrantFactory<TResponse, TRegistrant>
  where TRegistrant : IRegistrant
{
  /// <summary>
  /// Configures the mock to return the specified static value.
  /// </summary>
  /// <param name="response">The value to return when the request is intercepted.</param>
  /// <returns>The registrant for further configuration (e.g. <c>.WithCallback()</c>).</returns>
  TRegistrant WithReturnValue(TResponse response);
}
