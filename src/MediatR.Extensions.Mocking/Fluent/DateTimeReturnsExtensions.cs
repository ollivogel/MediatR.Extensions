namespace MediatR.Extensions.Mocking.Fluent;

/// <summary>
/// Convenience extensions for <see cref="DateTime"/> and <see cref="DateTimeOffset"/> response types.
/// All values are captured once at registration time, not re-evaluated per request.
/// </summary>
public static class DateTimeReturnsExtensions
{
  // ----------------------------------------------------------------
  //  DateTime
  // ----------------------------------------------------------------

  extension<TRegistrant>(IHandlerConfigurationRegistrantFactory<DateTime, TRegistrant> registrant)
    where TRegistrant : IRegistrant
  {
    /// <summary>
    /// Registers a mock that returns <see cref="DateTime.Now"/> (captured at registration time).
    /// </summary>
    public TRegistrant ReturnsNow() => registrant.WithReturnValue(DateTime.Now);

    /// <summary>
    /// Registers a mock that returns <see cref="DateTime.UtcNow"/> (captured at registration time).
    /// </summary>
    public TRegistrant ReturnsUtcNow() => registrant.WithReturnValue(DateTime.UtcNow);
  }

  // ----------------------------------------------------------------
  //  DateTimeOffset
  // ----------------------------------------------------------------

  extension<TRegistrant>(IHandlerConfigurationRegistrantFactory<DateTimeOffset, TRegistrant> registrant)
    where TRegistrant : IRegistrant
  {
    /// <summary>
    /// Registers a mock that returns <see cref="DateTimeOffset.Now"/> (captured at registration time).
    /// </summary>
    public TRegistrant ReturnsNow() => registrant.WithReturnValue(DateTimeOffset.Now);

    /// <summary>
    /// Registers a mock that returns <see cref="DateTimeOffset.UtcNow"/> (captured at registration time).
    /// </summary>
    public TRegistrant ReturnsUtcNow() => registrant.WithReturnValue(DateTimeOffset.UtcNow);
  }
}
