namespace Samples.FluentBuilder;

using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Samples.Conference;
using Xunit;

/// <summary>
/// Demonstrates the MediatR FluentBuilder Generator in action.
///
/// The FluentBuilder Generator creates chainable builder APIs for MediatR requests
/// decorated with [GenerateFluentBuilder]. Properties with [BuilderMethod] attributes
/// are grouped into semantically meaningful builder methods.
///
/// What you get for free:
///
///   Without builder:
///     await mediator.Send(new RegisterAttendeeCommand(conferenceId)
///     {
///         FirstName = "John", LastName = "Doe", Email = "john@example.com",
///         Company = "Acme Corp", JobTitle = "Senior Engineer",
///         IsVegetarian = true, Allergies = "nuts",
///         ArrivalDate = arrival, DepartureDate = departure, NeedsHotel = true,
///         PreferredWorkshops = { "Advanced C#", "Source Generators" }
///     });
///
///   With FluentBuilder:
///     await mediator.Build.Conference.RegisterAttendeeCommand(conferenceId)
///         .WithPersonalInfo("John", "Doe", "john@example.com")
///         .WithCompany("Acme Corp", department: "Engineering", jobTitle: "Senior Engineer")
///         .WithDietaryPreferences(isVegetarian: true, allergies: "nuts")
///         .WithTravel(arrival, departure, needsHotel: true)
///         .WithWorkshops(workshops)
///         .WithTShirtSize("L")
///         .Send();
///
/// The generated code is fully discoverable via IntelliSense and compile-time checked.
/// </summary>
public class FluentBuilderUsageTests : IAsyncLifetime
{
  private IMediator mediator = null!;
  private ServiceProvider serviceProvider = null!;

  public Task InitializeAsync()
  {
    serviceProvider = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RegisterAttendeeCommand>())
      .BuildServiceProvider();

    mediator = serviceProvider.GetRequiredService<IMediator>();
    return Task.CompletedTask;
  }

  public async Task DisposeAsync()
  {
    await serviceProvider.DisposeAsync();
  }

  // ================================================================
  //  1. FULL CHAIN — All builder methods in a single fluent call
  //
  //  This is the primary use case: setting 15+ properties through
  //  7 semantically grouped methods, then sending in one expression.
  // ================================================================

  [Fact]
  public async Task FullChain_AllBuilderMethods()
  {
    // The FluentBuilder groups 15 properties into 7 chainable methods.
    // Each [BuilderMethod("MethodName")] attribute defines which method
    // a property belongs to. Multiple properties with the same method
    // name are combined into a single method call.
    var conferenceId = Guid.NewGuid();
    var arrival = new DateTime(2026, 9, 15);
    var departure = new DateTime(2026, 9, 18);

    var result = await mediator.Build.Conference.RegisterAttendeeCommand(conferenceId)
      .WithPersonalInfo("John", "Doe", "john@example.com")
      .WithCompany("Acme Corp", department: "Engineering", jobTitle: "Senior Engineer")
      .WithDietaryPreferences(isVegetarian: true, allergies: "nuts")
      .WithTravel(arrival, departure, needsHotel: true)
      .WithWorkshops(["Advanced C#", "Source Generators Deep Dive"])
      .WithTShirtSize("L")
      .WithSpecialRequests("Wheelchair accessible seating")
      .Send();

    result.AttendeeName.Should().Be("John Doe");
    result.HotelBooked.Should().BeTrue();
    result.WorkshopCount.Should().Be(2);
    result.ConfirmationCode.Should().StartWith("CONF-");
  }

  // ================================================================
  //  2. PARAMETERLESS CONSTRUCTOR — mediator.Build.Conference.RegisterAttendeeCommand()
  //
  //  The generator creates entry points for ALL public constructors.
  //  RegisterAttendeeCommand has two: () and (Guid conferenceId).
  // ================================================================

  [Fact]
  public async Task ParameterlessConstructor_BuilderEntryPoint()
  {
    // Using the parameterless constructor — conferenceId defaults to Guid.Empty.
    // This demonstrates that the generator supports multiple constructors.
    var result = await mediator.Build.Conference.RegisterAttendeeCommand()
      .WithPersonalInfo("Jane", "Smith", "jane@example.com")
      .Send();

    result.AttendeeName.Should().Be("Jane Smith");
    result.ConfirmationCode.Should().StartWith("CONF-");
  }

  // ================================================================
  //  3. PARTIAL CHAINS — Only call the methods you need
  //
  //  Builder methods are optional. You can call any subset in any order.
  //  Unset properties keep their default values.
  // ================================================================

  [Fact]
  public async Task PartialChain_OnlyRequiredMethods()
  {
    // Only setting personal info — all other properties keep defaults.
    // This is useful when you only need a few fields.
    var conferenceId = Guid.NewGuid();

    var result = await mediator.Build.Conference.RegisterAttendeeCommand(conferenceId)
      .WithPersonalInfo("Alice", "Wonder", "alice@example.com")
      .Send();

    result.AttendeeName.Should().Be("Alice Wonder");
    result.HotelBooked.Should().BeFalse();
    result.WorkshopCount.Should().Be(0);
  }

  [Fact]
  public async Task PartialChain_SkipMiddleMethods()
  {
    // Methods can be called in any order and any subset.
    // Here we skip WithCompany and WithDietaryPreferences entirely.
    var conferenceId = Guid.NewGuid();

    var result = await mediator.Build.Conference.RegisterAttendeeCommand(conferenceId)
      .WithPersonalInfo("Bob", "Builder", "bob@example.com")
      .WithTravel(
        new DateTime(2026, 9, 15),
        new DateTime(2026, 9, 17),
        needsHotel: true)
      .WithTShirtSize("XL")
      .Send();

    result.AttendeeName.Should().Be("Bob Builder");
    result.HotelBooked.Should().BeTrue();
  }

  // ================================================================
  //  4. BUILD vs SEND — Two ways to finish the chain
  //
  //  .Send() sends the request via MediatR immediately.
  //  .Build() returns the raw request object for inspection or deferred sending.
  // ================================================================

  [Fact]
  public async Task Build_ReturnsRequestForInspection()
  {
    // .Build() returns the constructed RegisterAttendeeCommand without sending.
    // This is useful for assertions, logging, or passing to other code.
    var conferenceId = Guid.NewGuid();

    var request = mediator.Build.Conference.RegisterAttendeeCommand(conferenceId)
      .WithPersonalInfo("Carol", "Test", "carol@example.com")
      .WithCompany("TestCorp", department: "QA", jobTitle: "Tester")
      .WithDietaryPreferences(isVegetarian: false, allergies: null)
      .Build();

    // Inspect the constructed request
    request.ConferenceId.Should().Be(conferenceId);
    request.FirstName.Should().Be("Carol");
    request.LastName.Should().Be("Test");
    request.Email.Should().Be("carol@example.com");
    request.Company.Should().Be("TestCorp");
    request.Department.Should().Be("QA");
    request.JobTitle.Should().Be("Tester");
    request.IsVegetarian.Should().BeFalse();
    request.Allergies.Should().BeNull();

    // Now send it manually
    var result = await mediator.Send(request);
    result.AttendeeName.Should().Be("Carol Test");
  }

  // ================================================================
  //  5. GROUPED PARAMETERS — Multiple properties in one method
  //
  //  Properties sharing the same [BuilderMethod] name are combined.
  //  The Order attribute controls parameter position.
  // ================================================================

  [Fact]
  public async Task GroupedParameters_WithPersonalInfo_ThreeParams()
  {
    // WithPersonalInfo combines three properties:
    //   [BuilderMethod("WithPersonalInfo", Order = 0)] string FirstName
    //   [BuilderMethod("WithPersonalInfo", Order = 1)] string LastName
    //   [BuilderMethod("WithPersonalInfo", Order = 2)] string Email
    //
    // Generated: .WithPersonalInfo(string firstName, string lastName, string email)
    var request = mediator.Build.Conference.RegisterAttendeeCommand()
      .WithPersonalInfo("Max", "Mustermann", "max@mustermann.de")
      .Build();

    request.FirstName.Should().Be("Max");
    request.LastName.Should().Be("Mustermann");
    request.Email.Should().Be("max@mustermann.de");
  }

  [Fact]
  public async Task GroupedParameters_WithCompany_MixedNullability()
  {
    // WithCompany has required and optional parameters:
    //   [BuilderMethod("WithCompany", Order = 0)]                           string Company      → required
    //   [BuilderMethod("WithCompany", ParameterName = "department", Order = 1)] string? Department → nullable
    //   [BuilderMethod("WithCompany", ParameterName = "jobTitle", Order = 2)]   string? JobTitle   → nullable
    //
    // ParameterName overrides the default camelCase conversion.
    var request = mediator.Build.Conference.RegisterAttendeeCommand()
      .WithCompany("Microsoft", department: null, jobTitle: "Principal Engineer")
      .Build();

    request.Company.Should().Be("Microsoft");
    request.Department.Should().BeNull();
    request.JobTitle.Should().Be("Principal Engineer");
  }

  // ================================================================
  //  6. COLLECTION PARAMETERS — Lists work as builder parameters
  //
  //  Even complex types like List<string> are supported.
  // ================================================================

  [Fact]
  public async Task CollectionParameter_WithWorkshops()
  {
    // WithWorkshops accepts a List<string> parameter:
    //   [BuilderMethod("WithWorkshops", ParameterName = "preferredWorkshops", Order = 0)]
    //   public List<string> PreferredWorkshops { get; set; }
    var workshops = new List<string>
    {
      "Advanced C#",
      "Source Generators Deep Dive",
      "Blazor Performance Tuning"
    };

    var result = await mediator.Build.Conference.RegisterAttendeeCommand(Guid.NewGuid())
      .WithPersonalInfo("Dev", "Enthusiast", "dev@conf.io")
      .WithWorkshops(workshops)
      .Send();

    result.WorkshopCount.Should().Be(3);
  }

  // ================================================================
  //  7. MIXED TYPES — Different types in one builder method
  //
  //  Builder methods can combine bool, string, DateTime, and more.
  // ================================================================

  [Fact]
  public async Task MixedTypes_WithTravel_DateTimeAndBool()
  {
    // WithTravel mixes DateTime and bool in one method:
    //   [BuilderMethod("WithTravel", Order = 0)] DateTime ArrivalDate
    //   [BuilderMethod("WithTravel", Order = 1)] DateTime DepartureDate
    //   [BuilderMethod("WithTravel", ParameterName = "needsHotel", Order = 2)] bool NeedsHotel
    var request = mediator.Build.Conference.RegisterAttendeeCommand()
      .WithTravel(
        new DateTime(2026, 10, 1),
        new DateTime(2026, 10, 3),
        needsHotel: false)
      .Build();

    request.ArrivalDate.Should().Be(new DateTime(2026, 10, 1));
    request.DepartureDate.Should().Be(new DateTime(2026, 10, 3));
    request.NeedsHotel.Should().BeFalse();
  }

  [Fact]
  public async Task MixedTypes_WithDietaryPreferences_BoolAndNullableString()
  {
    // WithDietaryPreferences mixes bool and nullable string:
    //   [BuilderMethod("WithDietaryPreferences", Order = 0)] bool IsVegetarian
    //   [BuilderMethod("WithDietaryPreferences", ParameterName = "allergies", Order = 1)] string? Allergies
    var request = mediator.Build.Conference.RegisterAttendeeCommand()
      .WithDietaryPreferences(isVegetarian: true, allergies: "gluten, dairy")
      .Build();

    request.IsVegetarian.Should().BeTrue();
    request.Allergies.Should().Be("gluten, dairy");
  }
}
