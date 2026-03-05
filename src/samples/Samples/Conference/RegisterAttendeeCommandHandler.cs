namespace Samples.Conference;

using MediatR;

public class RegisterAttendeeCommandHandler
  : IRequestHandler<RegisterAttendeeCommand, AttendeeRegistrationResult>
{
  public Task<AttendeeRegistrationResult> Handle(
    RegisterAttendeeCommand request,
    CancellationToken cancellationToken)
  {
    return Task.FromResult(new AttendeeRegistrationResult
    {
      RegistrationId = Guid.NewGuid(),
      ConfirmationCode = $"CONF-{request.ConferenceId.ToString()[..8].ToUpper()}",
      AttendeeName = $"{request.FirstName} {request.LastName}",
      HotelBooked = request.NeedsHotel,
      WorkshopCount = request.PreferredWorkshops.Count
    });
  }
}
