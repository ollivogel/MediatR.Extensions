namespace Samples.Facade;

using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Samples.Person.Get;
using Xunit;

/// <summary>
/// Demonstrates the MediatR Facade Generator in action.
///
/// The Facade Generator scans all referenced assemblies for MediatR request types
/// (classes ending in Request/Command/Query that implement IRequest or IRequest{T})
/// and generates strongly-typed extension methods.
///
/// What you get for free:
///
///   Without facade:     await mediator.Send(new GetPersonByNameQuery("John") { City = "Berlin" });
///   With facade:        await mediator.Persons.Get.ByName("John", city: "Berlin");
///
/// The generated code is fully discoverable via IntelliSense and compile-time checked.
/// </summary>
public class FacadeUsageTests : IAsyncLifetime
{
  private IMediator mediator = null!;
  private ServiceProvider serviceProvider = null!;

  public Task InitializeAsync()
  {
    serviceProvider = new ServiceCollection()
      .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetPersonByNameQuery>())
      .BuildServiceProvider();

    mediator = serviceProvider.GetRequiredService<IMediator>();
    return Task.CompletedTask;
  }

  public async Task DisposeAsync()
  {
    await serviceProvider.DisposeAsync();
  }

  // ================================================================
  //  1. GROUPED QUERIES — mediator.Persons.Get.*
  //
  //  Requests with [MediatorGroup("Persons.Get")] generate a nested
  //  group: mediator.Persons.Get.{MethodName}(...)
  //
  //  [MediatorMethodName("ByName")] controls the method name.
  //  Without it, the method name would be the class name minus suffix
  //  (e.g. "GetPersonByName" from GetPersonByNameQuery).
  // ================================================================

  [Fact]
  public async Task Persons_Get_ByName_SingleConstructorParam()
  {
    // GetPersonByNameQuery has TWO constructors:
    //   (string name)                  ← picked (fewest params, not parameterless)
    //   (string name, string? country) ← ignored by Facade (FluentBuilder uses both)
    //
    // The Facade Generator always picks the constructor with the fewest parameters.
    // Only if no parameterized constructor exists does it fall back to parameterless.
    var result = await mediator.Persons.Get.ByName("John");

    result.FirstName.Should().Be("John");
  }

  [Fact]
  public async Task Persons_Get_ByName_WithOptionalFacadeParameter()
  {
    // City is a [FacadeParameter] on a nullable property (string?).
    // Nullable properties automatically become optional parameters with default null.
    //
    //   [FacadeParameter(Order = 0)]
    //   public string? City { get; set; }
    //
    // Generated: ByName(string name, string? city = null, ...)
    var result = await mediator.Persons.Get.ByName("John", city: "Berlin");

    result.City.Should().Be("Berlin");
  }

  [Fact]
  public async Task Persons_Get_ByEmail_ParameterlessConstructor()
  {
    // GetPersonByEmailQuery has ONLY a parameterless constructor.
    // All method parameters come from [FacadeParameter] attributes:
    //
    //   [FacadeParameter(Order = 0)] public string Email { get; set; }        → required
    //   [FacadeParameter(Order = 1)] public bool? IncludeInactive { get; set; } → optional
    //
    // Generated: ByEmail(string email, bool? includeInactive = null, ...)
    var result = await mediator.Persons.Get.ByEmail("john@example.com");

    result.Should().NotBeNull();
    result!.Email.Should().Be("john@example.com");
  }

  [Fact]
  public async Task Persons_Get_ByEmail_WithOptionalNullableBool()
  {
    // The bool? IncludeInactive parameter is optional (nullable → default null).
    // Named parameters make the intent clear at the call site.
    var result = await mediator.Persons.Get.ByEmail(
      "john@example.com",
      includeInactive: true);

    result.Should().NotBeNull();
  }

  // ================================================================
  //  2. GROUPED COMMANDS — mediator.Persons.*
  //
  //  Commands at the "Persons" group level appear directly on
  //  mediator.Persons.{MethodName}(...), not nested under a subgroup.
  // ================================================================

  [Fact]
  public async Task Persons_Create_SingleConstructorParam()
  {
    // CreateUserCommand has [MediatorMethodName("Create")].
    // Three constructors exist: (string email), (string, string), ().
    // The generator picks (string email) — fewest params, not parameterless.
    var result = await mediator.Persons.Create("new-user@example.com");

    result.Success.Should().BeTrue();
  }

  [Fact]
  public async Task Persons_Update_MultipleConstructorParams_PlusOptionalFacadeParams()
  {
    // UpdatePersonCommand has a constructor with THREE required parameters:
    //   (Guid id, string firstName, string lastName)
    //
    // Plus two optional [FacadeParameter]s:
    //   string? Email  → optional (nullable)
    //   string? Phone  → optional (nullable)
    //
    // Generated: Update(Guid id, string firstName, string lastName,
    //                    string? email = null, string? phone = null, ...)
    var personId = Guid.NewGuid();

    // Call with all parameters
    var result = await mediator.Persons.Update(
      personId,
      firstName: "John",
      lastName: "Doe",
      email: "john@example.com",
      phone: "+49 170 1234567");

    result.Success.Should().BeTrue();
    result.Message.Should().Contain("John Doe");
    result.Message.Should().Contain("email=john@example.com");
    result.Message.Should().Contain("phone=+49 170 1234567");
  }

  [Fact]
  public async Task Persons_Update_OnlyRequiredParams()
  {
    // Same command, but omitting the optional FacadeParameters.
    // The generated code allows this because email and phone default to null.
    var personId = Guid.NewGuid();

    var result = await mediator.Persons.Update(personId, "Jane", "Smith");

    result.Success.Should().BeTrue();
    result.Message.Should().Contain("Jane Smith");
    result.Message.Should().NotContain("email=");
  }

  [Fact]
  public async Task Persons_Delete_MixedConstructorTypes()
  {
    // DeletePersonCommand has two required constructor parameters of different types:
    //   (Guid id, bool permanent)
    //
    // Both become required in the generated facade.
    // No FacadeParameters — the simplest possible mixed-type signature.
    //
    // Generated: Delete(Guid id, bool permanent, ...)
    var personId = Guid.NewGuid();

    await mediator.Persons.Delete(personId, permanent: false);
  }

  // ================================================================
  //  3. CONFIGURE CALLBACK — Fine-tuning before Send
  //
  //  Every generated facade method includes a trailing optional
  //  Action<TRequest>? configure parameter. This gives full access
  //  to the request object for setting properties that aren't exposed
  //  as constructor params or FacadeParameters.
  // ================================================================

  [Fact]
  public async Task Persons_Get_ByName_WithConfigureCallback()
  {
    // The configure callback receives the fully constructed request object.
    // You can set any property, including ones not exposed as FacadeParameters.
    // Here we set Country (which is a regular property, not a FacadeParameter).
    var result = await mediator.Persons.Get.ByName(
      "John",
      configure: request =>
      {
        request.Country = "Germany";
        request.City = "Munich";
      });

    result.City.Should().Be("Munich");
  }

  // ================================================================
  //  4. CUSTOM EXTENSIONS — Extending the generated facade
  //
  //  The generated group classes (MediatorPersons, MediatorPersonsGet)
  //  are not sealed. You can add your own extension methods using
  //  C# 14's extension syntax. See: CustomPersonsExtensions.cs
  // ================================================================

  [Fact]
  public async Task Persons_CreateAndVerify_CustomComposedOperation()
  {
    // This method is NOT generated — we defined it ourselves in
    // CustomPersonsExtensions.cs. It composes two generated facade calls
    // (Create + Get.ByEmail) into a single high-level operation.
    //
    // Generated building blocks:
    //   mediator.Persons.Create(email)         — from CreateUserCommand
    //   mediator.Persons.Get.ByEmail(email)    — from GetPersonByEmailQuery
    //
    // Our custom extension:
    //   mediator.Persons.CreateAndVerify(email, firstName, lastName)
    var result = await mediator.Persons.CreateAndVerify(
      email: "john@example.com",
      firstName: "John",
      lastName: "Doe");

    result.Should().NotBeNull();
    result!.Email.Should().Be("john@example.com");
  }

  [Fact]
  public async Task Persons_Get_ByNameInCity_CustomConvenienceMethod()
  {
    // Custom extension on the nested MediatorPersonsGet group.
    // Wraps ByName() with a pre-filled city parameter.
    // See: CustomPersonsGetExtensions in CustomPersonsExtensions.cs
    var result = await mediator.Persons.Get.ByNameInCity("John", "Berlin");

    result.FirstName.Should().Be("John");
    result.City.Should().Be("Berlin");
  }
}
