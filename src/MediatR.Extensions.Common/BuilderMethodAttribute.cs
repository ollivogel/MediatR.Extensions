namespace MediatR.Extensions.Common;

/// <summary>
/// Marks a property to be exposed in the fluent builder.
/// Multiple properties can be grouped into the same builder method.
/// </summary>
/// <example>
/// <code>
/// [GenerateFluentBuilder]
/// public class CreateUserRequest : IRequest&lt;CreateUserResponse&gt;
/// {
///     // ParameterName defaults to "firstName" (camelCase)
///     [BuilderMethod("WithName", Order = 0)]
///     public string FirstName { get; set; }
///
///     // ParameterName explicitly specified
///     [BuilderMethod("WithName", ParameterName = "lastName", Order = 1)]
///     public string LastName { get; set; }
///
///     // Single parameter, defaults to "email" (camelCase)
///     [BuilderMethod("WithEmail")]
///     public string Email { get; set; }
/// }
///
/// // Generates:
/// builder.WithName(firstName: "Max", lastName: "Mustermann")
/// builder.WithEmail(email: "max@example.com")
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class BuilderMethodAttribute : Attribute
{
  /// <summary>
  /// The name of the builder method (e.g. "WithName", "WithEmail").
  /// </summary>
  public string MethodName { get; }

  /// <summary>
  /// The parameter name in the builder method.
  /// If not specified, the property name in camelCase is used.
  /// </summary>
  public string? ParameterName { get; set; }

  /// <summary>
  /// The parameter order when multiple properties belong to the same method.
  /// Default: 0
  /// </summary>
  public int Order { get; set; }

  public BuilderMethodAttribute(string methodName)
  {
    MethodName = methodName;
  }
}
