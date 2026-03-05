namespace MediatR.Extensions.Mocking.Registration.Extensions;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides the <c>AddMediatorMocking()</c> extension method for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
  extension(IServiceCollection services)
  {
    /// <summary>
    /// Registers the mock mediator pipeline behavior and configures mock handlers.
    /// </summary>
    /// <param name="configure">Optional action to configure runtime mode and mock registrations.</param>
    /// <returns>The service collection for chaining.</returns>
    public IServiceCollection AddMediatorMocking(Action<MediatorMockingConfiguration>? configure = null)
    {
      ArgumentNullException.ThrowIfNull(services);

      return services
        .AddTransient(typeof(IPipelineBehavior<,>), typeof(MockMediatorBehavior<,>))
        .AddSingleton<MediatorMockingConfiguration>(_ =>
        {
          var m = new MediatorMockingConfiguration();
          configure?.Invoke(m);
          return m;
        });
    }
  }
}
