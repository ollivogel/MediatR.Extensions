namespace MediatR.Extensions.Mocking;

using MediatR.Extensions.Mocking.Extensions;

/// <summary>
/// MediatR pipeline behavior that intercepts requests and returns mock responses
/// based on the configured <see cref="HandlerConfigurationStore"/>.
/// Registered automatically by <c>AddMediatorMocking()</c>.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class MockMediatorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : IBaseRequest
{
  private readonly RuntimeMode runtimeMode;
  private readonly HandlerConfigurationStore store;

  public MockMediatorBehavior(MediatorMockingConfiguration mockMediatorConfiguration)
  {
    this.runtimeMode = mockMediatorConfiguration.RuntimeMode;
    this.store = mockMediatorConfiguration.MockRegistration;
  }

  private bool TryResolveHandlerConfiguration(out HandlerConfigurationBase? handlerConfiguration)
  {
    if (this.store.TryGet<TRequest>(out handlerConfiguration))
    {
      return true;
    }

    if (this.runtimeMode == RuntimeMode.Fallback)
    {
      handlerConfiguration = HandlerConfigurationBase.CreateDefault(typeof(TRequest));
      return true;
    }

    return false;
  }

  public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
  {
    if (!TryResolveHandlerConfiguration(out var hc))
    {
      EnsurePassThroughMode();
      return await next(cancellationToken);
    }

    if (request is IRequest)
    {
      hc!.InvokeCallback(request);
      ThrowIfConfigured(hc);
      return default!;
    }

    if (request is IRequest<TResponse>)
    {
      return ProcessWithReturnValue(request, hc!);
    }

    throw new InvalidOperationException(
      $"Request type '{typeof(TRequest).FullName}' does not implement IRequest or IRequest<TResponse>.");
  }

  private static TResponse ProcessWithReturnValue(TRequest request, HandlerConfigurationBase handlerConfiguration)
  {
    handlerConfiguration.InvokeCallback(request);
    ThrowIfConfigured(handlerConfiguration);

    if (handlerConfiguration is not HandlerConfigurationBaseWithReturnValue withReturnValue)
    {
      throw new InvalidOperationException(
        $"Handler configuration for '{typeof(TRequest).FullName}' does not provide a return value.");
    }

    object? returnValue;

    if (withReturnValue.ResponseSequence is not null)
    {
      returnValue = withReturnValue.GetNextSequenceValue();
    }
    else
    {
      returnValue = withReturnValue.GetReturnValue(request);
    }

    return returnValue is null ? default! : (TResponse)returnValue;
  }

  private static void ThrowIfConfigured(HandlerConfigurationBase handlerConfiguration)
  {
    if (handlerConfiguration.ExceptionToThrow is { } ex)
    {
      throw ex;
    }
  }

  private void EnsurePassThroughMode()
  {
    if (this.runtimeMode != RuntimeMode.PassThrough)
    {
      throw new InvalidOperationException(
        $"No mock handler registered for '{typeof(TRequest).FullName}'. " +
        $"Register a mock or use RuntimeMode.PassThrough to fall through to real handlers.");
    }
  }
}
