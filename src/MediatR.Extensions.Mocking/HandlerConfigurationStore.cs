namespace MediatR.Extensions.Mocking;

/// <summary>
/// Store for mock handler configurations, keyed by MediatR request type.
/// Populated during test setup via the generated <c>MockRegistration</c> fluent API.
/// </summary>
public sealed class HandlerConfigurationStore
{
  private Dictionary<Type, HandlerConfigurationBase> handlerConfigurations = [];

  /// <summary>
  /// Registers a handler configuration for the specified request type.
  /// Replaces any previously registered configuration for the same type.
  /// </summary>
  /// <typeparam name="TRequest">The MediatR request type to mock.</typeparam>
  /// <param name="handlerConfiguration">The configuration defining the mock behavior.</param>
  public void Register<TRequest>(HandlerConfigurationBase handlerConfiguration) where TRequest : IBaseRequest
  {
    this.handlerConfigurations[typeof(TRequest)] = handlerConfiguration;
  }

  /// <summary>
  /// Attempts to retrieve the handler configuration for the specified request type.
  /// </summary>
  /// <typeparam name="TRequest">The MediatR request type to look up.</typeparam>
  /// <param name="hr">The handler configuration if found; otherwise <c>null</c>.</param>
  /// <returns><c>true</c> if a configuration was found; otherwise <c>false</c>.</returns>
  public bool TryGet<TRequest>(out HandlerConfigurationBase? hr) where TRequest : IBaseRequest
  {
    return this.handlerConfigurations.TryGetValue(typeof(TRequest), out hr);
  }
}
