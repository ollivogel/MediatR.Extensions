namespace MediatR.Extensions.Common;

/// <summary>
/// Marks a MediatR request for which a fluent builder should be generated.
/// The builder provides an intuitive, chainable API for creating and sending requests.
/// </summary>
/// <example>
/// <code>
/// [GenerateFluentBuilder]
/// public class CreateUserRequest : IRequest&lt;CreateUserResponse&gt;
/// {
///     public string FirstName { get; set; }
///     public string LastName { get; set; }
/// }
///
/// // Usage:
/// var result = await mediator.CreateUser()
///     .WithFirstName("Max")
///     .WithLastName("Mustermann")
///     .ExecuteAsync();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateFluentBuilderAttribute : Attribute
{
}
