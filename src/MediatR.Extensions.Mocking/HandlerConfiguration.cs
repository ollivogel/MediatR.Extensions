namespace MediatR.Extensions.Mocking;

/// <summary>
/// Handler configuration for void MediatR requests (commands without return values).
/// Stores an optional callback that is invoked when the request is intercepted.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type implementing <see cref="IRequest"/>.</typeparam>
public sealed class HandlerConfiguration<TRequest> : HandlerConfigurationBase where TRequest : IRequest
{
  /// <summary>
  /// Optional callback invoked with the intercepted request before returning.
  /// Use this to capture or assert on request values in tests.
  /// </summary>
  public Action<TRequest>? Callback { get; set; }

  public override void InvokeCallback(object o)
  {
    if (o is not TRequest request)
    {
      throw new ArgumentException(
        $"Expected request of type '{typeof(TRequest).FullName}', but received '{o.GetType().FullName}'.");
    }

    this.Callback?.Invoke(request);
  }
}

/// <summary>
/// Base class for all handler configurations. Provides shared functionality
/// for exception simulation and callback invocation.
/// </summary>
public abstract class HandlerConfigurationBase
{
  /// <summary>
  /// When set, the mock behavior throws this exception instead of returning a value.
  /// The callback (if any) is still invoked before the exception is thrown.
  /// </summary>
  public Exception? ExceptionToThrow { get; set; }

  /// <summary>
  /// Invokes the configured callback with the given request object.
  /// </summary>
  /// <param name="o">The intercepted MediatR request.</param>
  public abstract void InvokeCallback(object o);
}

/// <summary>
/// Handler configuration for MediatR requests that return a value.
/// Supports static return values, dynamic response factories, response sequences, and callbacks.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type implementing <see cref="IRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
/// <param name="response">The default static return value.</param>
public sealed class HandlerConfigurationWithReturnValue<TRequest, TResponse>(TResponse response)
  : HandlerConfigurationBaseWithReturnValue
  where TRequest : IRequest<TResponse>
{
  /// <summary>
  /// The static return value. Used when no <see cref="ResponseFactory"/> or <see cref="HandlerConfigurationBaseWithReturnValue.ResponseSequence"/> is configured.
  /// </summary>
  public TResponse ReturnValue { get; set; } = response;

  /// <summary>
  /// Optional callback invoked with the intercepted request before returning the response.
  /// Use this to capture or assert on request values in tests.
  /// </summary>
  public Action<TRequest>? Callback { get; set; }

  /// <summary>
  /// When set, this factory is called with the request to produce a dynamic response.
  /// Takes precedence over <see cref="ReturnValue"/>.
  /// </summary>
  public Func<TRequest, TResponse>? ResponseFactory { get; set; }

  public override void InvokeCallback(object o)
  {
    if (o is not TRequest request)
    {
      throw new ArgumentException(
        $"Expected request of type '{typeof(TRequest).FullName}', but received '{o.GetType().FullName}'.");
    }

    Callback?.Invoke(request);
  }

  public override object? GetReturnValue() => this.ReturnValue;

  public override object? GetReturnValue(object request)
  {
    if (this.ResponseFactory is not null && request is TRequest typedRequest)
    {
      return this.ResponseFactory(typedRequest);
    }

    return this.ReturnValue;
  }

  public override object? GetNextSequenceValue()
  {
    if (this.ResponseSequence is null || this.ResponseSequence.Length == 0)
    {
      return this.ReturnValue;
    }

    var index = Interlocked.Increment(ref sequenceIndex) - 1;
    return this.ResponseSequence[index % this.ResponseSequence.Length];
  }
}

/// <summary>
/// Abstract base class for handler configurations that produce a return value.
/// Provides infrastructure for static values, response factories, and response sequences.
/// </summary>
public abstract class HandlerConfigurationBaseWithReturnValue : HandlerConfigurationBase
{
  /// <summary>
  /// When set, successive calls cycle through these values (round-robin).
  /// Takes precedence over both the static return value and the response factory.
  /// </summary>
  public object?[]? ResponseSequence { get; set; }

  protected int sequenceIndex;

  public abstract object? GetReturnValue();

  /// <summary>
  /// Gets the return value using the response factory if available, otherwise falls back to the static value.
  /// </summary>
  public abstract object? GetReturnValue(object request);

  /// <summary>
  /// Gets the next value from the response sequence (round-robin).
  /// </summary>
  public abstract object? GetNextSequenceValue();
}
