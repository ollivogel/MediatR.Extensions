# MediatR.Extensions

[![NuGet](https://img.shields.io/nuget/v/MediatR.Extensions.Mocking?label=NuGet)](https://www.nuget.org/packages/MediatR.Extensions.Mocking)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 10+](https://img.shields.io/badge/.NET-10%2B-purple)]()

**Strongly-typed source generators for MediatR** -- Mock Registration, Facade API, and Fluent Builder.

[MediatR](https://github.com/jbogard/MediatR) is the most popular mediator/CQRS implementation for .NET. This library extends it with three Roslyn source generators that eliminate boilerplate and add compile-time safety. They share a single attribute package and can be used independently or together.

> **Requirements:** .NET 10+ / C# 14 (uses the new `extension(...)` syntax)

---

### Before & After

```csharp
// TESTING -- Before: manual mock wiring for every handler
var handler = new Mock<IRequestHandler<PlaceOrderCommand, OrderResponse>>();
handler.Setup(h => h.Handle(It.IsAny<PlaceOrderCommand>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(new OrderResponse { Status = "Mocked" });
services.AddTransient(_ => handler.Object);
// ... repeat for every handler in your test

// TESTING -- After: one-liner, IntelliSense-driven
c.MockRegistration.Orders.PlaceOrder
    .WithReturnValue(new OrderResponse { Status = "Mocked" });
```

```csharp
// PRODUCTION CODE -- Before: verbose, not discoverable
await mediator.Send(new GetPersonByNameQuery("John") { City = "Berlin" });

// PRODUCTION CODE -- After: typed, navigable API
await mediator.Persons.Get.ByName("John", city: "Berlin");
```

```csharp
// COMPLEX REQUESTS -- Before: wall of property assignments
await mediator.Send(new RegisterAttendeeCommand(conferenceId)
{
    FirstName = "John", LastName = "Doe", Email = "john@example.com",
    Company = "Acme Corp", JobTitle = "Senior Engineer",
    IsVegetarian = true, Allergies = "nuts", /* ... 8 more properties ... */
});

// COMPLEX REQUESTS -- After: semantic, chainable builder
await mediator.Build.Conference.RegisterAttendee(conferenceId)
    .WithPersonalInfo("John", "Doe", "john@example.com")
    .WithCompany("Acme Corp", jobTitle: "Senior Engineer")
    .WithDietaryPreferences(isVegetarian: true, allergies: "nuts")
    .Send();
```

---

### Quick Start

```bash
# Pick what you need:
dotnet add package MediatR.Extensions.Common          # Attributes for your request types
dotnet add package MediatR.Extensions.Mocking               # Mock runtime (test projects)
dotnet add package MediatR.Extensions.Mocking.Generator     # Mock source generator (test projects)
dotnet add package MediatR.Extensions.Facade                # Facade source generator
dotnet add package MediatR.Extensions.FluentBuilder          # FluentBuilder source generator
```

Source generators must be referenced as analyzers:

```xml
<PackageReference Include="MediatR.Extensions.Mocking.Generator"
                  OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

---

### How It Works

All three packages are **Roslyn source generators**. They analyze your MediatR request types at compile time and generate strongly-typed C# code. There is zero runtime reflection and zero code generation at runtime. The generated API shows up in IntelliSense immediately after building.

The generators discover requests automatically by scanning for classes implementing `IRequest` or `IRequest<T>` whose name ends with `Request`, `Command`, or `Query`.

---

### Feature 1: Mock Registration

The Mocking package intercepts MediatR requests in tests via a pipeline behavior. The source generator creates a strongly-typed registration API -- no more `Mock<IRequestHandler<...>>`.

```csharp
var provider = new ServiceCollection()
    .AddMediatorMocking(c =>
    {
        c.MockRegistration.Orders.PlaceOrder
            .WithReturnValue(new OrderResponse { Status = "Mocked" });
    })
    .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>())
    .BuildServiceProvider();

var result = await provider.GetRequiredService<IMediator>()
    .Send(new PlaceOrderCommand { ProductName = "Widget" });
// result.Status == "Mocked" -- real handler was never called
```

#### Runtime Modes

| Mode | Behavior for unmocked requests |
|---|---|
| **Strict** (default) | Throws `InvalidOperationException` |
| **PassThrough** | Falls through to the real handler |
| **Fallback** | Returns `new TResponse()` (default-constructed) |

```csharp
c.RuntimeMode = RuntimeMode.PassThrough;
```

#### Return Values

```csharp
// Static return value
c.MockRegistration.Orders.PlaceOrder
    .WithReturnValue(new OrderResponse { Status = "OK" });

// Generic shortcut for any return type
c.MockRegistration.Orders.PlaceOrder.Returns(new OrderResponse { Status = "OK" });

// ReturnsDefault() returns default(T) for any return type
c.MockRegistration.Orders.PlaceOrder.ReturnsDefault();
```

Based on the handler's return type, additional convenience methods are available. For example, handlers returning `bool` get `ReturnsTrue()` and `ReturnsFalse()`, `string` handlers get `ReturnsEmpty()`, `Guid` handlers get `ReturnsNewGuid()`, and `DateTime`/`DateTimeOffset` handlers get `ReturnsNow()` and `ReturnsUtcNow()`. You can also write your own extension methods following the same pattern.

#### Dynamic Responses

Return different values based on the incoming request:

```csharp
c.MockRegistration.Orders.PlaceOrder
    .Returns(request => new OrderResponse
    {
        ProductName = request.ProductName,
        Status = request.Amount > 100 ? "NeedsApproval" : "Confirmed"
    });

var cheap = await mediator.Send(new PlaceOrderCommand { Amount = 5 });     // "Confirmed"
var expensive = await mediator.Send(new PlaceOrderCommand { Amount = 1500 }); // "NeedsApproval"
```

#### Response Sequences

Return different values on successive calls (round-robin):

```csharp
c.MockRegistration.Orders.GetOrderById
    .ReturnsSequence(
        new OrderResponse { Status = "Pending" },
        new OrderResponse { Status = "Processing" },
        new OrderResponse { Status = "Shipped" });

var first  = await mediator.Send(new GetOrderByIdQuery(id)); // "Pending"
var second = await mediator.Send(new GetOrderByIdQuery(id)); // "Processing"
var third  = await mediator.Send(new GetOrderByIdQuery(id)); // "Shipped"
var fourth = await mediator.Send(new GetOrderByIdQuery(id)); // "Pending" (wraps around)
```

#### Exception Simulation

```csharp
// Throw a specific exception
c.MockRegistration.Orders.PlaceOrder
    .Throws(new InvalidOperationException("Out of stock"));

// Typed shortcut
c.MockRegistration.Orders.PlaceOrder.Throws<TimeoutException>();

// Works on void commands too
c.MockRegistration.Orders.CancelOrder.Throws<UnauthorizedAccessException>();
```

#### Callbacks -- Inspect and Assert

Combine with any of the above -- callbacks run before the response is returned (or exception thrown):

```csharp
c.MockRegistration.Orders.PlaceOrder
    .Returns(request => new OrderResponse { Status = "OK" })
    .WithCallback(request =>
    {
        request.ProductName.Should().Be("Expected Product");
    });

// Even with Throws -- callback still fires before the exception
c.MockRegistration.Orders.PlaceOrder
    .Throws(new InvalidOperationException("Boom"))
    .WithCallback(request => captured = request);
```

---

### Feature 2: Facade Generator

Generates typed extension methods on `IMediator`, turning verbose `Send()` calls into a clean, discoverable, navigable API.

**Before:**
```csharp
var result = await mediator.Send(new GetPersonByNameQuery("John") { City = "Berlin" });
```

**After:**
```csharp
var result = await mediator.Persons.Get.ByName("John", city: "Berlin");
```

#### How to use it

Decorate your requests with `[MediatorGroup]` and optionally `[MediatorMethodName]`:

```csharp
[MediatorGroup("Persons")]
[MediatorMethodName("Update")]
public class UpdatePersonCommand : IRequest<UpdateResult>
{
    public UpdatePersonCommand(Guid id) { Id = id; }
    public Guid Id { get; }

    [FacadeParameter(Order = 0)]
    public string FirstName { get; set; }       // required (non-nullable)

    [FacadeParameter(Order = 1)]
    public string LastName { get; set; }        // required (non-nullable)

    [FacadeParameter(Order = 2)]
    public string? Email { get; set; }          // optional (nullable)

    [FacadeParameter(Order = 3)]
    public string? Phone { get; set; }          // optional (nullable)
}

// Generated: mediator.Persons.Update(id, "John", "Doe", email: "j@x.com")
//   - Constructor params first, then [FacadeParameter] in Order
//   - Non-nullable = required, nullable = optional (can be omitted)
```

- **Constructor parameters** become required method parameters
- **`[FacadeParameter]`** properties become additional parameters -- use `Order` to control position, nullable properties become optional
- **`[MediatorMethodName]`** overrides the auto-generated name (default: class name without suffix)
- Every method includes a trailing `Action<TRequest>? configure` callback for edge cases

#### Nested Groups

Dot-separated group names create a navigable hierarchy -- your MediatR requests become a structured API:

```csharp
[MediatorGroup("Persons")]          // -> mediator.Persons.Create(...)
public class CreatePersonCommand : IRequest<PersonResult> { ... }

[MediatorGroup("Persons.Get")]      // -> mediator.Persons.Get.ByName(...)
public class GetPersonByNameQuery : IRequest<PersonResult> { ... }

[MediatorGroup("Persons.Get")]      // -> mediator.Persons.Get.ById(...)
public class GetPersonByIdQuery : IRequest<PersonResult> { ... }

[MediatorGroup("Orders")]           // -> mediator.Orders.Place(...)
public class PlaceOrderCommand : IRequest<OrderResult> { ... }

[MediatorGroup("Orders.Returns")]   // -> mediator.Orders.Returns.Request(...)
public class RequestReturnCommand : IRequest<ReturnResult> { ... }
```

Navigate your entire domain via IntelliSense: `mediator.` -> pick domain -> pick action.

#### Extend Generated Classes with Your Own Methods

The generated group classes (`MediatorPersons`, `MediatorOrders`, ...) are not sealed. You can add your own convenience methods using C# 14's `extension(...)` syntax -- composing multiple generated calls into higher-level operations:

```csharp
public static class PersonsConvenienceExtensions
{
    extension(MediatorPersons persons)
    {
        public async Task<PersonResult?> CreateAndVerify(
            string email, string firstName, string lastName)
        {
            await persons.Create(email, firstName, lastName);
            return await persons.Get.ByEmail(email);
        }

        public async Task<bool> Exists(string email)
        {
            return await persons.Get.ByEmail(email) is not null;
        }
    }
}

// Usage -- your custom methods sit right next to the generated ones:
await mediator.Persons.CreateAndVerify("john@x.com", "John", "Doe");
var exists = await mediator.Persons.Exists("john@x.com");
```

Your entire domain API in one place: generated methods + your custom business logic, all discoverable via `mediator.Persons.`.

---

### Feature 3: Fluent Builder Generator

Generates chainable builder APIs for requests with many parameters. Multiple properties can be grouped into semantically meaningful builder methods.

**Before:**
```csharp
await mediator.Send(new RegisterAttendeeCommand(conferenceId)
{
    FirstName = "John", LastName = "Doe", Email = "john@example.com",
    Company = "Acme Corp", JobTitle = "Senior Engineer",
    IsVegetarian = true, Allergies = "nuts",
    ArrivalDate = arrival, DepartureDate = departure, NeedsHotel = true
});
```

**After:**
```csharp
await mediator.Build.Conference.RegisterAttendee(conferenceId)
    .WithPersonalInfo("John", "Doe", "john@example.com")
    .WithCompany("Acme Corp", jobTitle: "Senior Engineer")
    .WithDietaryPreferences(isVegetarian: true, allergies: "nuts")
    .WithTravel(arrival, departure, needsHotel: true)
    .Send();
```

#### How to use it

Decorate your request with `[GenerateFluentBuilder]` and group properties with `[BuilderMethod]`:

```csharp
[MediatorGroup("Conference")]
[GenerateFluentBuilder]
public class RegisterAttendeeCommand : IRequest<AttendeeRegistrationResult>
{
    public RegisterAttendeeCommand(Guid conferenceId) { ConferenceId = conferenceId; }

    public Guid ConferenceId { get; set; }

    [BuilderMethod("WithPersonalInfo", Order = 0)]
    public string FirstName { get; set; } = string.Empty;

    [BuilderMethod("WithPersonalInfo", Order = 1)]
    public string LastName { get; set; } = string.Empty;

    [BuilderMethod("WithPersonalInfo", Order = 2)]
    public string Email { get; set; } = string.Empty;
}
```

- Properties with the **same method name** are combined into one builder method
- **`Order`** controls parameter position within a method
- `.Send()` sends immediately, `.Build()` returns the raw request for inspection
- Builder methods are optional -- call any subset in any order

---

### Package Architecture

```
MediatR.Extensions.Common (netstandard2.0)
  Attributes: [MediatorGroup], [MediatorMethodName], [FacadeParameter],
              [GenerateFluentBuilder], [BuilderMethod]
       |
       |-- MediatR.Extensions.Mocking (net10.0, runtime)
       |     MockMediatorBehavior, HandlerConfigurationStore
       |
       |-- MediatR.Extensions.Mocking.Generator (net6.0, source generator)
       |     Generates: MockRegistration extensions
       |
       |-- MediatR.Extensions.Facade (net6.0, source generator)
       |     Generates: mediator.Group.Method(...) extensions
       |
       +-- MediatR.Extensions.FluentBuilder (net6.0, source generator)
             Generates: mediator.Build.Group.Request().With*().Send()
```

All five packages can be used independently. Only install what you need.

---

### License

[MIT](LICENSE)
