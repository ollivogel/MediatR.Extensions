namespace Samples.Conference;

using MediatR;
using MediatR.Extensions.Common;

/// <summary>
/// Registers an attendee for a conference.
///
/// This request demonstrates the FluentBuilder Generator with many parameters
/// grouped into semantically meaningful builder methods:
///
///   await mediator.Build.Conference.RegisterAttendeeCommand(conferenceId)
///       .WithPersonalInfo("John", "Doe", "john@example.com")
///       .WithCompany("Acme Corp", jobTitle: "Senior Engineer")
///       .WithDietaryPreferences(isVegetarian: true, allergies: "nuts")
///       .WithTravel(arrival, departure, needsHotel: true)
///       .WithWorkshops("Advanced C#", "Source Generators Deep Dive")
///       .Send();
///
/// Without the FluentBuilder, you'd have to set 15+ properties manually:
///
///   await mediator.Send(new RegisterAttendeeCommand(conferenceId)
///   {
///       FirstName = "John", LastName = "Doe", Email = "john@example.com",
///       Company = "Acme Corp", JobTitle = "Senior Engineer",
///       IsVegetarian = true, Allergies = "nuts",
///       ArrivalDate = ..., DepartureDate = ..., NeedsHotel = true,
///       PreferredWorkshops = { "Advanced C#", "Source Generators Deep Dive" }
///   });
/// </summary>
[MediatorGroup("Conference")]
[GenerateFluentBuilder]
public class RegisterAttendeeCommand : IRequest<AttendeeRegistrationResult>
{
  // ----------------------------------------------------------------
  //  Constructor — required context for the registration
  // ----------------------------------------------------------------

  /// <summary>
  /// Parameterless constructor.
  /// The FluentBuilder generates a builder entry point without arguments.
  /// </summary>
  public RegisterAttendeeCommand()
  {
  }

  /// <summary>
  /// Constructor with conference ID.
  /// The FluentBuilder generates a second entry point that accepts the conference ID.
  /// Both constructors are available: mediator.Build.Conference.RegisterAttendeeCommand()
  /// and mediator.Build.Conference.RegisterAttendeeCommand(conferenceId).
  /// </summary>
  public RegisterAttendeeCommand(Guid conferenceId)
  {
    ConferenceId = conferenceId;
  }

  public Guid ConferenceId { get; set; }

  // ----------------------------------------------------------------
  //  Personal Info — grouped into .WithPersonalInfo(firstName, lastName, email)
  //
  //  Multiple properties with the same [BuilderMethod] name are combined
  //  into a single method. Order controls parameter position.
  // ----------------------------------------------------------------

  [BuilderMethod("WithPersonalInfo", Order = 0)]
  public string FirstName { get; set; } = string.Empty;

  [BuilderMethod("WithPersonalInfo", Order = 1)]
  public string LastName { get; set; } = string.Empty;

  [BuilderMethod("WithPersonalInfo", Order = 2)]
  public string Email { get; set; } = string.Empty;

  // ----------------------------------------------------------------
  //  Company Info — grouped into .WithCompany(company, department, jobTitle)
  //
  //  ParameterName overrides the default camelCase name.
  // ----------------------------------------------------------------

  [BuilderMethod("WithCompany", Order = 0)]
  public string Company { get; set; } = string.Empty;

  [BuilderMethod("WithCompany", ParameterName = "department", Order = 1)]
  public string? Department { get; set; }

  [BuilderMethod("WithCompany", ParameterName = "jobTitle", Order = 2)]
  public string? JobTitle { get; set; }

  // ----------------------------------------------------------------
  //  Dietary Preferences — .WithDietaryPreferences(isVegetarian, allergies)
  //
  //  Mix of bool and nullable string shows different types in one method.
  // ----------------------------------------------------------------

  [BuilderMethod("WithDietaryPreferences", Order = 0)]
  public bool IsVegetarian { get; set; }

  [BuilderMethod("WithDietaryPreferences", ParameterName = "allergies", Order = 1)]
  public string? Allergies { get; set; }

  // ----------------------------------------------------------------
  //  Travel Details — .WithTravel(arrivalDate, departureDate, needsHotel)
  //
  //  DateTime + bool mix. Shows that any type works in builder methods.
  // ----------------------------------------------------------------

  [BuilderMethod("WithTravel", Order = 0)]
  public DateTime ArrivalDate { get; set; }

  [BuilderMethod("WithTravel", Order = 1)]
  public DateTime DepartureDate { get; set; }

  [BuilderMethod("WithTravel", ParameterName = "needsHotel", Order = 2)]
  public bool NeedsHotel { get; set; }

  // ----------------------------------------------------------------
  //  Workshop Selection — .WithWorkshops(preferredWorkshops)
  //
  //  Collection type — even List<string> works as a builder parameter.
  // ----------------------------------------------------------------

  [BuilderMethod("WithWorkshops", ParameterName = "preferredWorkshops", Order = 0)]
  public List<string> PreferredWorkshops { get; set; } = [];

  // ----------------------------------------------------------------
  //  T-Shirt Size — .WithTShirtSize(size)
  //
  //  Single property = single-parameter builder method (no grouping needed).
  // ----------------------------------------------------------------

  [BuilderMethod("WithTShirtSize", ParameterName = "size", Order = 0)]
  public string TShirtSize { get; set; } = "M";

  // ----------------------------------------------------------------
  //  Special Requests — .WithSpecialRequests(notes)
  //
  //  Nullable string — the builder method accepts null.
  // ----------------------------------------------------------------

  [BuilderMethod("WithSpecialRequests", ParameterName = "notes", Order = 0)]
  public string? SpecialRequests { get; set; }
}
