namespace MediatR.Extensions.Common;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class FacadeParameterAttribute : Attribute
{
  /// <summary>
  /// The parameter name to use in the generated method. If null, uses camelCase of the property name.
  /// </summary>
  public string? Name { get; set; }

  /// <summary>
  /// Whether the parameter is optional in the generated method. Defaults to false for non-nullable types, true for nullable types.
  /// </summary>
  public bool IsOptional { get; set; }

  /// <summary>
  /// The default value for the parameter if it's optional. Only used when IsOptional is true.
  /// </summary>
  public object? DefaultValue { get; set; }

  /// <summary>
  /// The order of this parameter among other facade parameters. Parameters are sorted by Order first, then by Name.
  /// Facade parameters always come after constructor parameters.
  /// </summary>
  public int Order { get; set; }
}
