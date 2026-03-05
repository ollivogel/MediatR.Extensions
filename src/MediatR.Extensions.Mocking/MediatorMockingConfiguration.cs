namespace MediatR.Extensions.Mocking;

/// <summary>
/// Root configuration object passed to <c>AddMediatorMocking()</c>.
/// Use <see cref="RuntimeMode"/> to control behavior for unmocked requests
/// and <see cref="MockRegistration"/> to configure mock responses.
/// </summary>
/// <example>
/// <code>
/// services.AddMediatorMocking(c =>
/// {
///     c.RuntimeMode = RuntimeMode.PassThrough;
///     c.MockRegistration.Orders.PlaceOrder
///         .WithReturnValue(new OrderResponse { Status = "Mocked" });
/// });
/// </code>
/// </example>
public sealed class MediatorMockingConfiguration
{
  /// <summary>
  /// Controls behavior when a request has no registered mock handler.
  /// Defaults to <see cref="RuntimeMode.Strict"/>.
  /// </summary>
  public RuntimeMode RuntimeMode { get; set; } = RuntimeMode.Strict;

  /// <summary>
  /// The handler configuration store. The source generator extends this with
  /// strongly-typed properties for each discovered request type.
  /// </summary>
  public HandlerConfigurationStore MockRegistration { get; } = new();
}
