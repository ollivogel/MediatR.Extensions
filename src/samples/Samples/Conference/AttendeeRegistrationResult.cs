namespace Samples.Conference;

public class AttendeeRegistrationResult
{
  public Guid RegistrationId { get; set; }
  public string ConfirmationCode { get; set; } = string.Empty;
  public string AttendeeName { get; set; } = string.Empty;
  public bool HotelBooked { get; set; }
  public int WorkshopCount { get; set; }
}
