namespace MediatR.Extensions.Mocking;

public enum RuntimeMode
{
  /// <summary>
  /// Only accepts mocked dependencies. Throws <see cref="InvalidOperationException"/>
  /// if no mock handler is registered for a request.
  /// </summary>
  Strict,

  /// <summary>
  /// Only accepts mocked dependencies. If no mock is registered, creates a default
  /// handler configuration with default return values.
  /// </summary>
  Fallback,

  /// <summary>
  /// If no mock is registered, passes the request through to the real MediatR handler.
  /// </summary>
  PassThrough,
}
